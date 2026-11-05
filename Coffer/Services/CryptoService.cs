using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Coffer.Models;
using Konscious.Security.Cryptography;

namespace Coffer.Services;

public class CryptoService
{
    public const int KeySize = 32;      // 256-bit keys
    public const int NonceSize = 12;    // 96-bit GCM nonce
    public const int TagSize = 16;      // 128-bit GCM tag

    // Kept as-is so vaults created by the original app remain readable.
    private const string EntryKeyInfo = "heavenly-lock-entry";

    public byte[] GenerateDek()
    {
        byte[] dek = new byte[KeySize];
        RandomNumberGenerator.Fill(dek);
        return dek;
    }

    public byte[] DeriveKey(byte[] password, byte[] salt, int iterations, int memoryKb, int parallelism)
    {
        var argon2 = new Argon2id(password)
        {
            Salt = salt,
            DegreeOfParallelism = parallelism,
            MemorySize = memoryKb,
            Iterations = iterations
        };

        return argon2.GetBytes(KeySize);
    }

    public (byte[] CipherText, byte[] Nonce, byte[] Tag) WrapDek(byte[] dek, byte[] kek)
    {
        byte[] nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        byte[] cipherText = new byte[dek.Length];
        byte[] tag = new byte[TagSize];

        using var aes = new AesGcm(kek, TagSize);
        aes.Encrypt(nonce, dek, cipherText, tag);

        return (cipherText, nonce, tag);
    }

    public byte[] UnwrapDek(byte[] cipherText, byte[] nonce, byte[] tag, byte[] kek)
    {
        byte[] dek = new byte[cipherText.Length];

        using var aes = new AesGcm(kek, TagSize);
        aes.Decrypt(nonce, cipherText, tag, dek);

        return dek;
    }

    public (byte[] CipherText, byte[] Nonce, byte[] Tag) EncryptVault(Vault vault, byte[] dek)
    {
        byte[] plainText = JsonSerializer.SerializeToUtf8Bytes(vault);
        try
        {
            return EncryptBytes(plainText, dek);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plainText);
        }
    }

    public Vault DecryptVault(byte[] cipherText, byte[] nonce, byte[] tag, byte[] dek)
    {
        byte[] plainText = DecryptBytes(cipherText, nonce, tag, dek);
        try
        {
            return JsonSerializer.Deserialize<Vault>(plainText)
                ?? throw new CryptographicException("The vault payload could not be read.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plainText);
        }
    }

    public (byte[] CipherText, byte[] Nonce, byte[] Tag) EncryptPassword(string password, byte[] dek, Guid entryId)
    {
        byte[] subKey = DeriveEntryKey(dek, entryId);
        byte[] plainText = Encoding.UTF8.GetBytes(password);
        try
        {
            return EncryptBytes(plainText, subKey);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plainText);
            CryptographicOperations.ZeroMemory(subKey);
        }
    }

    public string DecryptPassword(byte[] cipherText, byte[] nonce, byte[] tag, byte[] dek, Guid entryId)
    {
        byte[] subKey = DeriveEntryKey(dek, entryId);
        byte[] plainText = DecryptBytes(cipherText, nonce, tag, subKey);
        try
        {
            return Encoding.UTF8.GetString(plainText);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plainText);
            CryptographicOperations.ZeroMemory(subKey);
        }
    }

    public byte[] GenerateSalt(int size = 32)
    {
        byte[] salt = new byte[size];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    public void SecureClear(byte[]? bytes)
    {
        if (bytes is null) return;
        CryptographicOperations.ZeroMemory(bytes);
    }

    private static byte[] DeriveEntryKey(byte[] dek, Guid entryId)
    {
        byte[] entryIdBytes = Encoding.UTF8.GetBytes(entryId.ToString("N"));
        return HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            ikm: dek,
            outputLength: KeySize,
            salt: entryIdBytes,
            info: Encoding.UTF8.GetBytes(EntryKeyInfo));
    }

    private static (byte[] CipherText, byte[] Nonce, byte[] Tag) EncryptBytes(byte[] plainText, byte[] key)
    {
        byte[] nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        byte[] cipherText = new byte[plainText.Length];
        byte[] tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plainText, cipherText, tag);

        return (cipherText, nonce, tag);
    }

    private static byte[] DecryptBytes(byte[] cipherText, byte[] nonce, byte[] tag, byte[] key)
    {
        if (nonce.Length != NonceSize || tag.Length != TagSize)
            throw new CryptographicException("Encrypted data is malformed.");

        byte[] plainText = new byte[cipherText.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, cipherText, tag, plainText);

        return plainText;
    }
}