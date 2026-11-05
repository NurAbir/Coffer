using System.Text.Json.Serialization;

namespace Coffer.Models;

public class VaultMetadata
{
    public const int CurrentVersion = 2;

    [JsonPropertyName("version")]
    public int Version { get; set; } = CurrentVersion;

    // Master password KDF parameters.
    [JsonPropertyName("salt")]
    public byte[] Salt { get; set; } = Array.Empty<byte>();

    [JsonPropertyName("argon2Iterations")]
    public int Argon2Iterations { get; set; } = 3;

    [JsonPropertyName("argon2MemoryKB")]
    public int Argon2MemoryKB { get; set; } = 65536;

    [JsonPropertyName("argon2Parallelism")]
    public int Argon2Parallelism { get; set; } = 4;

    // Data encryption key, wrapped with the master-password KEK.
    [JsonPropertyName("encryptedDek")]
    public byte[] EncryptedDek { get; set; } = Array.Empty<byte>();

    [JsonPropertyName("dekNonce")]
    public byte[] DekNonce { get; set; } = Array.Empty<byte>();

    [JsonPropertyName("dekTag")]
    public byte[] DekTag { get; set; } = Array.Empty<byte>();

    // Recovery phrase KDF parameters.
    [JsonPropertyName("recoverySalt")]
    public byte[] RecoverySalt { get; set; } = Array.Empty<byte>();

    [JsonPropertyName("recoveryArgon2Iterations")]
    public int RecoveryArgon2Iterations { get; set; } = 3;

    [JsonPropertyName("recoveryArgon2MemoryKB")]
    public int RecoveryArgon2MemoryKB { get; set; } = 65536;

    [JsonPropertyName("recoveryArgon2Parallelism")]
    public int RecoveryArgon2Parallelism { get; set; } = 4;

    // Data encryption key, wrapped with the recovery-phrase KEK.
    [JsonPropertyName("recoveryEncryptedDek")]
    public byte[] RecoveryEncryptedDek { get; set; } = Array.Empty<byte>();

    [JsonPropertyName("recoveryDekNonce")]
    public byte[] RecoveryDekNonce { get; set; } = Array.Empty<byte>();

    [JsonPropertyName("recoveryDekTag")]
    public byte[] RecoveryDekTag { get; set; } = Array.Empty<byte>();

    // The container-level nonce and tag for the encrypted vault payload.
    // These are kept here for backwards compatibility with vaults created by
    // earlier versions; the outer container values are always authoritative.
    [JsonPropertyName("vaultNonce")]
    public byte[] VaultNonce { get; set; } = Array.Empty<byte>();

    [JsonPropertyName("vaultTag")]
    public byte[] VaultTag { get; set; } = Array.Empty<byte>();
}