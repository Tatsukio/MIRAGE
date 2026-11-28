using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MIRAGE.Shared.IO;

/// <summary>Reads and writes the JSON files of the tools with System.Text.Json.</summary>
public static class JsonFile
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        WriteIndented = true,
        IndentCharacter = ' ',
        IndentSize = 2,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        // Keep non-ASCII text (translations) readable in the file.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Reads a file. Throws <see cref="JsonException"/> or an IO exception.</summary>
    public static T Load<T>(string p_FilePath)
    {
        using FileStream stream = new(p_FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return JsonSerializer.Deserialize<T>(stream, Options) ?? throw new JsonException($"{p_FilePath} contains 'null'.");
    }

    public static void Save<T>(string p_FilePath, T p_Value)
    {
        string json = JsonSerializer.Serialize(p_Value, Options);
        AtomicFile.WriteAllBytes(p_FilePath, new UTF8Encoding(false).GetBytes(json + "\r\n"));
    }
}