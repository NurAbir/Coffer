using System.Text.Json.Serialization;

namespace Coffer.Models;

public class Vault
{
    [JsonPropertyName("metadata")]
    public VaultMetadata Metadata { get; set; } = new();

    [JsonPropertyName("entries")]
    public List<VaultEntry> Entries { get; set; } = new();
}