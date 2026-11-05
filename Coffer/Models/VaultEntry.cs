using System.Text.Json.Serialization;

namespace Coffer.Models;

public class VaultEntry
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("service")]
    public string Service { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    // Encrypted with an HKDF sub-key derived from the vault DEK and this entry's id.
    [JsonPropertyName("encryptedPassword")]
    public byte[] EncryptedPassword { get; set; } = Array.Empty<byte>();

    [JsonPropertyName("passwordNonce")]
    public byte[] PasswordNonce { get; set; } = Array.Empty<byte>();

    [JsonPropertyName("passwordTag")]
    public byte[] PasswordTag { get; set; } = Array.Empty<byte>();

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("modifiedAt")]
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}