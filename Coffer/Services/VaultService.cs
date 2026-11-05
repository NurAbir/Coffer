using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Coffer.Models;

namespace Coffer.Services;

public sealed class InvalidPasswordException : Exception
{
    public InvalidPasswordException() : base("Invalid master password.") { }
}

public sealed class InvalidRecoveryPhraseException : Exception
{
    public InvalidRecoveryPhraseException() : base("Invalid recovery phrase.") { }
}

public sealed class VaultSession
{
    public VaultSession(Vault vault, byte[] dek)
    {
        Vault = vault;
        Dek = dek;
    }

    public Vault Vault { get; }
    public byte[] Dek { get; }
}

public class VaultService
{
    public const int MinimumPasswordLength = 8;

    private const string AppFolderName = "Coffer";
    private const string LegacyFolderName = "HeavenlyLock";
    private const string VaultFileName = "vault.coffer";
    private const string LegacyVaultFileName = "vault.heavenly";
    private const string LockoutFileName = "lockout.json";

    private const int Argon2Iterations = 3;
    private const int Argon2MemoryKb = 65536;
    private const int Argon2Parallelism = 4;

    private readonly CryptoService _crypto;
    private readonly PasswordGenerator _generator;

    public VaultService(CryptoService crypto, PasswordGenerator generator, string? dataDirectory = null)
    {
        _crypto = crypto;
        _generator = generator;

        bool useDefaultLocation = string.IsNullOrWhiteSpace(dataDirectory);

        DataDirectory = useDefaultLocation
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppFolderName)
            : dataDirectory!;

        VaultPath = Path.Combine(DataDirectory, VaultFileName);
        LockoutPath = Path.Combine(DataDirectory, LockoutFileName);
        BackupPath = VaultPath + ".bak";

        Directory.CreateDirectory(DataDirectory);

        // Legacy migration only applies to the real user-data location.
        if (useDefaultLocation)
            MigrateLegacyVault();
    }

    public string DataDirectory { get; }
    public string VaultPath { get; }
    public string BackupPath { get; }
    public string LockoutPath { get; }
    public bool MigrationHappened { get; private set; }

    public bool VaultExists() => File.Exists(VaultPath);

    public string CreateVault(string masterPassword)
    {
        ValidateNewPassword(masterPassword);

        byte[] dek = _crypto.GenerateDek();
        byte[] passwordBytes = Encoding.UTF8.GetBytes(masterPassword);

        try
        {
            var metadata = new VaultMetadata
            {
                Version = VaultMetadata.CurrentVersion,
                Salt = _crypto.GenerateSalt(),
                Argon2Iterations = Argon2Iterations,
                Argon2MemoryKB = Argon2MemoryKb,
                Argon2Parallelism = Argon2Parallelism
            };

            byte[] masterKek = _crypto.DeriveKey(
                passwordBytes,
                metadata.Salt,
                metadata.Argon2Iterations,
                metadata.Argon2MemoryKB,
                metadata.Argon2Parallelism);

            try
            {
                var (encryptedDek, dekNonce, dekTag) = _crypto.WrapDek(dek, masterKek);
                metadata.EncryptedDek = encryptedDek;
                metadata.DekNonce = dekNonce;
                metadata.DekTag = dekTag;
            }
            finally
            {
                _crypto.SecureClear(masterKek);
            }

            string recoveryPhrase = _generator.GenerateRecoveryPhrase(12);
            byte[] recoveryBytes = Encoding.UTF8.GetBytes(NormalizeRecoveryPhrase(recoveryPhrase));

            try
            {
                metadata.RecoverySalt = _crypto.GenerateSalt();
                metadata.RecoveryArgon2Iterations = Argon2Iterations;
                metadata.RecoveryArgon2MemoryKB = Argon2MemoryKb;
                metadata.RecoveryArgon2Parallelism = Argon2Parallelism;

                byte[] recoveryKek = _crypto.DeriveKey(
                    recoveryBytes,
                    metadata.RecoverySalt,
                    metadata.RecoveryArgon2Iterations,
                    metadata.RecoveryArgon2MemoryKB,
                    metadata.RecoveryArgon2Parallelism);

                try
                {
                    var (encryptedDek, dekNonce, dekTag) = _crypto.WrapDek(dek, recoveryKek);
                    metadata.RecoveryEncryptedDek = encryptedDek;
                    metadata.RecoveryDekNonce = dekNonce;
                    metadata.RecoveryDekTag = dekTag;
                }
                finally
                {
                    _crypto.SecureClear(recoveryKek);
                }
            }
            finally
            {
                _crypto.SecureClear(recoveryBytes);
            }

            var vault = new Vault { Metadata = metadata };
            var (cipherText, vaultNonce, vaultTag) = _crypto.EncryptVault(vault, dek);

            metadata.VaultNonce = vaultNonce;
            metadata.VaultTag = vaultTag;

            WriteContainer(new VaultContainer
            {
                Metadata = metadata,
                EncryptedData = cipherText
            });

            return recoveryPhrase;
        }
        finally
        {
            _crypto.SecureClear(dek);
            _crypto.SecureClear(passwordBytes);
        }
    }

    public VaultSession Unlock(string masterPassword)
    {
        var container = ReadContainer();
        ValidateMetadata(container.Metadata);

        byte[] kek = DeriveMasterKek(container.Metadata, masterPassword);
        byte[]? dek = null;

        try
        {
            try
            {
                dek = _crypto.UnwrapDek(
                    container.Metadata.EncryptedDek,
                    container.Metadata.DekNonce,
                    container.Metadata.DekTag,
                    kek);
            }
            catch (CryptographicException)
            {
                throw new InvalidPasswordException();
            }

            Vault vault = DecryptAndSync(container, dek);
            return new VaultSession(vault, dek);
        }
        catch
        {
            if (dek is not null)
                _crypto.SecureClear(dek);
            throw;
        }
        finally
        {
            _crypto.SecureClear(kek);
        }
    }

    public VaultSession RecoverVault(string recoveryPhrase)
    {
        var container = ReadContainer();
        ValidateMetadata(container.Metadata);

        byte[] recoveryBytes = Encoding.UTF8.GetBytes(NormalizeRecoveryPhrase(recoveryPhrase));
        byte[] kek = _crypto.DeriveKey(
            recoveryBytes,
            container.Metadata.RecoverySalt,
            container.Metadata.RecoveryArgon2Iterations,
            container.Metadata.RecoveryArgon2MemoryKB,
            container.Metadata.RecoveryArgon2Parallelism);

        byte[]? dek = null;

        try
        {
            try
            {
                dek = _crypto.UnwrapDek(
                    container.Metadata.RecoveryEncryptedDek,
                    container.Metadata.RecoveryDekNonce,
                    container.Metadata.RecoveryDekTag,
                    kek);
            }
            catch (CryptographicException)
            {
                throw new InvalidRecoveryPhraseException();
            }

            Vault vault = DecryptAndSync(container, dek);
            return new VaultSession(vault, dek);
        }
        catch
        {
            if (dek is not null)
                _crypto.SecureClear(dek);
            throw;
        }
        finally
        {
            _crypto.SecureClear(kek);
            _crypto.SecureClear(recoveryBytes);
        }
    }

    public Vault OpenWithDek(byte[] dek)
    {
        var container = ReadContainer();
        ValidateMetadata(container.Metadata);
        return DecryptAndSync(container, dek);
    }

    /// <summary>
    /// Re-keys the vault with a new master password and returns a fresh
    /// recovery phrase. Used after recovery.
    /// </summary>
    public string ResetMasterPasswordWithDek(byte[] dek, string newPassword)
    {
        ValidateNewPassword(newPassword);

        var container = ReadContainer();
        ValidateMetadata(container.Metadata);

        Vault vault = DecryptAndSync(container, dek);
        VaultMetadata metadata = container.Metadata;

        byte[] passwordBytes = Encoding.UTF8.GetBytes(newPassword);
        try
        {
            metadata.Salt = _crypto.GenerateSalt();
            byte[] newKek = _crypto.DeriveKey(
                passwordBytes,
                metadata.Salt,
                metadata.Argon2Iterations,
                metadata.Argon2MemoryKB,
                metadata.Argon2Parallelism);

            try
            {
                var (encryptedDek, dekNonce, dekTag) = _crypto.WrapDek(dek, newKek);
                metadata.EncryptedDek = encryptedDek;
                metadata.DekNonce = dekNonce;
                metadata.DekTag = dekTag;
            }
            finally
            {
                _crypto.SecureClear(newKek);
            }
        }
        finally
        {
            _crypto.SecureClear(passwordBytes);
        }

        string newRecoveryPhrase = _generator.GenerateRecoveryPhrase(12);
        byte[] recoveryBytes = Encoding.UTF8.GetBytes(NormalizeRecoveryPhrase(newRecoveryPhrase));

        try
        {
            metadata.RecoverySalt = _crypto.GenerateSalt();
            byte[] newRecoveryKek = _crypto.DeriveKey(
                recoveryBytes,
                metadata.RecoverySalt,
                metadata.RecoveryArgon2Iterations,
                metadata.RecoveryArgon2MemoryKB,
                metadata.RecoveryArgon2Parallelism);

            try
            {
                var (encryptedDek, dekNonce, dekTag) = _crypto.WrapDek(dek, newRecoveryKek);
                metadata.RecoveryEncryptedDek = encryptedDek;
                metadata.RecoveryDekNonce = dekNonce;
                metadata.RecoveryDekTag = dekTag;
            }
            finally
            {
                _crypto.SecureClear(newRecoveryKek);
            }
        }
        finally
        {
            _crypto.SecureClear(recoveryBytes);
        }

        vault.Metadata = metadata;

        var (cipherText, vaultNonce, vaultTag) = _crypto.EncryptVault(vault, dek);
        metadata.VaultNonce = vaultNonce;
        metadata.VaultTag = vaultTag;

        WriteContainer(new VaultContainer
        {
            Metadata = metadata,
            EncryptedData = cipherText
        });

        return newRecoveryPhrase;
    }

    public VaultMetadata ChangeMasterPassword(string oldPassword, string newPassword)
    {
        ValidateNewPassword(newPassword);

        VaultSession session = Unlock(oldPassword);

        try
        {
            var container = ReadContainer();
            VaultMetadata metadata = container.Metadata;

            byte[] passwordBytes = Encoding.UTF8.GetBytes(newPassword);
            try
            {
                metadata.Salt = _crypto.GenerateSalt();
                byte[] newKek = _crypto.DeriveKey(
                    passwordBytes,
                    metadata.Salt,
                    metadata.Argon2Iterations,
                    metadata.Argon2MemoryKB,
                    metadata.Argon2Parallelism);

                try
                {
                    var (encryptedDek, dekNonce, dekTag) = _crypto.WrapDek(session.Dek, newKek);
                    metadata.EncryptedDek = encryptedDek;
                    metadata.DekNonce = dekNonce;
                    metadata.DekTag = dekTag;
                }
                finally
                {
                    _crypto.SecureClear(newKek);
                }
            }
            finally
            {
                _crypto.SecureClear(passwordBytes);
            }

            session.Vault.Metadata = metadata;

            var (cipherText, vaultNonce, vaultTag) = _crypto.EncryptVault(session.Vault, session.Dek);
            metadata.VaultNonce = vaultNonce;
            metadata.VaultTag = vaultTag;

            WriteContainer(new VaultContainer
            {
                Metadata = metadata,
                EncryptedData = cipherText
            });

            return metadata;
        }
        finally
        {
            _crypto.SecureClear(session.Dek);
        }
    }

    public void SaveVault(Vault vault, byte[] dek)
    {
        // The on-disk metadata is authoritative for KDF and wrapped-key values.
        // Reusing it prevents an in-memory copy from accidentally reverting a
        // recent password change.
        var container = ReadContainer();
        vault.Metadata = container.Metadata;

        var (cipherText, nonce, tag) = _crypto.EncryptVault(vault, dek);
        vault.Metadata.VaultNonce = nonce;
        vault.Metadata.VaultTag = tag;

        WriteContainer(new VaultContainer
        {
            Metadata = vault.Metadata,
            EncryptedData = cipherText
        });
    }

    public void ExportBackup(string destinationPath)
    {
        if (!VaultExists())
            throw new FileNotFoundException("Vault not found.");

        string? directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.Copy(VaultPath, destinationPath, overwrite: true);
    }

    public void DeleteVault()
    {
        TryDelete(VaultPath);
        TryDelete(BackupPath);
        TryDelete(VaultPath + ".tmp");
        TryDelete(LockoutPath);
    }

    public VaultMetadata LoadVaultMetadata() => ReadContainer().Metadata;

    public static string NormalizeRecoveryPhrase(string phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase)) return string.Empty;

        return string.Join(
            ' ',
            phrase.ToLowerInvariant().Split(
                new[] { ' ', '\t', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries));
    }

    public static void ValidateNewPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinimumPasswordLength)
            throw new ArgumentException($"Password must be at least {MinimumPasswordLength} characters.", nameof(password));
    }

    private Vault DecryptAndSync(VaultContainer container, byte[] dek)
    {
        Vault vault = _crypto.DecryptVault(
            container.EncryptedData,
            container.Metadata.VaultNonce,
            container.Metadata.VaultTag,
            dek);

        // The outer container values are authoritative. Syncing them into the
        // decrypted metadata keeps subsequent saves consistent.
        vault.Metadata = container.Metadata;
        vault.Metadata.VaultNonce = container.Metadata.VaultNonce;
        vault.Metadata.VaultTag = container.Metadata.VaultTag;
        return vault;
    }

    private byte[] DeriveMasterKek(VaultMetadata metadata, string password)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        try
        {
            return _crypto.DeriveKey(
                passwordBytes,
                metadata.Salt,
                metadata.Argon2Iterations,
                metadata.Argon2MemoryKB,
                metadata.Argon2Parallelism);
        }
        finally
        {
            _crypto.SecureClear(passwordBytes);
        }
    }

    private VaultContainer ReadContainer()
    {
        if (!File.Exists(VaultPath))
            throw new FileNotFoundException("Vault not found.", VaultPath);

        byte[] json = File.ReadAllBytes(VaultPath);
        if (json.Length == 0)
            throw new InvalidDataException("The vault file is empty.");

        VaultContainer? container;
        try
        {
            container = JsonSerializer.Deserialize<VaultContainer>(json);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            throw new InvalidDataException("The vault file is not a valid Coffer vault.", ex);
        }

        if (container is null || container.Metadata is null || container.EncryptedData is null)
            throw new InvalidDataException("The vault file is incomplete.");

        return container;
    }

    private void WriteContainer(VaultContainer container)
    {
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(container);
        string tempPath = VaultPath + ".tmp";

        Directory.CreateDirectory(DataDirectory);

        using (var stream = new FileStream(
                   tempPath,
                   FileMode.Create,
                   FileAccess.Write,
                   FileShare.None,
                   4096,
                   FileOptions.WriteThrough))
        {
            stream.Write(json, 0, json.Length);
            stream.Flush(flushToDisk: true);
        }

        if (File.Exists(VaultPath))
        {
            // Keep the previous good vault as a one-generation backup.
            File.Copy(VaultPath, BackupPath, overwrite: true);
            File.Move(tempPath, VaultPath, overwrite: true);
        }
        else
        {
            File.Move(tempPath, VaultPath);
        }
    }

    private void MigrateLegacyVault()
    {
        if (File.Exists(VaultPath)) return;

        string legacyDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            LegacyFolderName);

        string legacyVault = Path.Combine(legacyDirectory, LegacyVaultFileName);
        if (!File.Exists(legacyVault)) return;

        try
        {
            File.Copy(legacyVault, VaultPath, overwrite: false);

            string legacyLockout = Path.Combine(legacyDirectory, LockoutFileName);
            if (File.Exists(legacyLockout) && !File.Exists(LockoutPath))
                File.Copy(legacyLockout, LockoutPath, overwrite: false);

            MigrationHappened = true;
        }
        catch
        {
            // If migration fails, the user can still point Coffer at the old
            // file manually. Never destroy the original.
        }
    }

    private static void ValidateMetadata(VaultMetadata metadata)
    {
        if (metadata.Version is < 1 or > VaultMetadata.CurrentVersion)
            throw new InvalidDataException($"Unsupported vault version: {metadata.Version}.");

        Require(metadata.Salt, 16, 128, "salt");
        Require(metadata.EncryptedDek, 32, 32, "wrapped DEK");
        Require(metadata.DekNonce, 12, 12, "DEK nonce");
        Require(metadata.DekTag, 16, 16, "DEK tag");
        Require(metadata.RecoverySalt, 16, 128, "recovery salt");
        Require(metadata.RecoveryEncryptedDek, 32, 32, "wrapped recovery DEK");
        Require(metadata.RecoveryDekNonce, 12, 12, "recovery DEK nonce");
        Require(metadata.RecoveryDekTag, 16, 16, "recovery DEK tag");
        Require(metadata.VaultNonce, 12, 12, "vault nonce");
        Require(metadata.VaultTag, 16, 16, "vault tag");

        ValidateKdf(metadata.Argon2Iterations, metadata.Argon2MemoryKB, metadata.Argon2Parallelism);
        ValidateKdf(metadata.RecoveryArgon2Iterations, metadata.RecoveryArgon2MemoryKB, metadata.RecoveryArgon2Parallelism);
    }

    private static void ValidateKdf(int iterations, int memoryKb, int parallelism)
    {
        // Bounds protect against a malicious vault file demanding absurd work.
        if (iterations is < 1 or > 20)
            throw new InvalidDataException("Invalid Argon2 iteration count.");
        if (memoryKb is < 8192 or > 1_048_576)
            throw new InvalidDataException("Invalid Argon2 memory cost.");
        if (parallelism is < 1 or > 32)
            throw new InvalidDataException("Invalid Argon2 parallelism.");
    }

    private static void Require(byte[]? value, int minLength, int maxLength, string name)
    {
        if (value is null || value.Length < minLength || value.Length > maxLength)
            throw new InvalidDataException($"Invalid {name} in vault metadata.");
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Best effort cleanup.
        }
    }

    private sealed class VaultContainer
    {
        [JsonPropertyName("metadata")]
        public VaultMetadata Metadata { get; set; } = new();

        [JsonPropertyName("data")]
        public byte[] EncryptedData { get; set; } = Array.Empty<byte>();
    }
}