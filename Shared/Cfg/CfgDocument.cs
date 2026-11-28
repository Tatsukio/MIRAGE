using MIRAGE.Shared.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace MIRAGE.Shared.Cfg;

/// <summary>
/// A ParaWorld config file (Settings.cfg and the other files in the same tree format).
/// <para>
/// Changes are made in the original text, so everything that isn't changed stays byte for byte as it was:
/// formatting, line ends, a UTF-8 byte order mark, characters of any code page. Values are decoded with UTF-8
/// when the file has a byte order mark, otherwise with the ANSI code page of the system (as the game does).
/// </para>
/// <para>Paths are node names separated by '/'. The root name may be given or left out:
/// <c>Root/Global/Language</c> and <c>Global/Language</c> are the same.</para>
/// </summary>
public sealed class CfgDocument
{
    private static readonly byte[] s_utf8Bom = [0xEF, 0xBB, 0xBF];
    private static readonly Encoding s_raw = Encoding.Latin1;

    private readonly Encoding m_valueEncoding;
    private List<int> m_lineStarts = [];

    private CfgDocument(string p_RawText, bool p_HasBom, string? p_FilePath)
    {
        Text = p_RawText;
        HasBom = p_HasBom;
        FilePath = p_FilePath;
        m_valueEncoding = p_HasBom ? new UTF8Encoding(false) : GetAnsiEncoding();
        Root = Reparse();
    }

    /// <summary>The file, if the document was loaded from one.</summary>
    public string? FilePath { get; private set; }

    public bool HasBom { get; }

    public CfgNode Root { get; private set; }

    /// <summary>True after a change that isn't saved yet.</summary>
    public bool IsModified { get; private set; }

    /// <summary>The text as stored: one char per byte (Latin-1), without the byte order mark.</summary>
    internal string Text { get; private set; }

    private string NewLine => (Text.Contains("\r\n", StringComparison.Ordinal) || !Text.Contains('\n')) ? "\r\n" : "\n";

    /// <summary>Reads and parses a file. Throws <see cref="CfgParseException"/> or an IO exception.</summary>
    public static CfgDocument Load(string p_FilePath)
    {
        byte[] bytes = ReadAllBytesShared(p_FilePath);
        return FromBytes(bytes, Path.GetFullPath(p_FilePath));
    }

    /// <summary>Like <see cref="Load"/>, but returns the error message instead of throwing.</summary>
    public static bool TryLoad(string p_FilePath, out CfgDocument? p_Document, out string p_Error)
    {
        try
        {
            p_Document = Load(p_FilePath);
            p_Error = string.Empty;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CfgParseException)
        {
            p_Document = null;
            p_Error = ex.Message;
            return false;
        }
    }

    public static CfgDocument FromBytes(byte[] p_Bytes, string? p_FilePath = null)
    {
        ArgumentNullException.ThrowIfNull(p_Bytes);
        bool hasBom = p_Bytes.AsSpan().StartsWith(s_utf8Bom);
        string raw = s_raw.GetString(p_Bytes, hasBom ? 3 : 0, p_Bytes.Length - (hasBom ? 3 : 0));
        return new CfgDocument(raw, hasBom, p_FilePath);
    }

    /// <summary>Parses text that is already decoded (no byte order mark, ANSI code page on save).</summary>
    public static CfgDocument Parse(string p_Text)
    {
        ArgumentNullException.ThrowIfNull(p_Text);
        return FromBytes(GetAnsiEncoding().GetBytes(p_Text));
    }

    public static string[] SplitPath(string p_Path)
    {
        ArgumentNullException.ThrowIfNull(p_Path);
        return p_Path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>The node at the path, or null.</summary>
    public CfgNode? Find(string p_Path)
    {
        CfgNode? node = Root;
        foreach (string part in RelativeParts(p_Path))
        {
            node = node.FindChild(part);
            if (node is null)
            {
                return null;
            }
        }
        return node;
    }

    /// <summary>The value at the path; null if the node doesn't exist, "" if it has no value.</summary>
    public string? Get(string p_Path)
    {
        CfgNode? node = Find(p_Path);
        return node is null ? null : (node.Value ?? string.Empty);
    }

    /// <summary>
    /// Sets the value at the path. Missing nodes on the path are created. Returns false if the value was already
    /// the same (nothing changed).
    /// </summary>
    public bool Set(string p_Path, string p_Value)
    {
        ArgumentNullException.ThrowIfNull(p_Value);
        if (p_Value.AsSpan().IndexOfAny('\'', '\r', '\n') >= 0)
        {
            throw new ArgumentException("A config value can't contain ' or line breaks.", nameof(p_Value));
        }
        string[] parts = RelativeParts(p_Path);
        foreach (string part in parts)
        {
            ValidateName(part);
        }
        string encoded = EncodeRaw(p_Value);

        CfgNode node = Root;
        int depth = 0;
        while (depth < parts.Length)
        {
            CfgNode? child = node.FindChild(parts[depth]);
            if (child is null)
            {
                break;
            }
            node = child;
            depth++;
        }

        if (depth == parts.Length)
        {
            if (node.HasValue)
            {
                if (string.Equals(node.RawValue, encoded, StringComparison.Ordinal))
                {
                    return false;
                }
                if (node.IsValueQuoted)
                {
                    Replace(node.ValueStart, node.ValueEnd, encoded);
                }
                else
                {
                    Replace(node.ValueStart, node.ValueEnd, $"'{encoded}'");
                }
            }
            else
            {
                Replace(node.NameEnd, node.NameEnd, $" = '{encoded}'");
            }
            return true;
        }

        string newLine = NewLine;
        string unit = DetectIndentUnit();
        string parentIndent = GetLineIndent(node.NameStart) ?? string.Empty;
        string lines = BuildChain(parts.AsSpan(depth), encoded, parentIndent + unit, unit, newLine);

        if (node.HasBlock)
        {
            int lineStart = GetLineStart(node.CloseBrace);
            if (IsBlank(lineStart, node.CloseBrace) && (lineStart > node.OpenBrace))
            {
                Replace(lineStart, lineStart, lines);
            }
            else
            {
                Replace(node.CloseBrace, node.CloseBrace, newLine + lines + parentIndent);
            }
        }
        else
        {
            Replace(node.End, node.End, " {" + newLine + lines + parentIndent + "}");
        }
        return true;
    }

    /// <summary>Removes the node at the path with everything below it. Returns false if it didn't exist.</summary>
    public bool Remove(string p_Path)
    {
        CfgNode? node = Find(p_Path);
        if (node is null)
        {
            return false;
        }
        if (node.Parent is null)
        {
            throw new InvalidOperationException("The root node can't be removed.");
        }

        int start = node.NameStart;
        int end = node.End;
        int lineStart = GetLineStart(start);
        if (IsBlank(lineStart, start))
        {
            int p = end;
            while ((p < Text.Length) && (Text[p] is ' ' or '\t' or ','))
            {
                p++;
            }
            if ((p < Text.Length) && (Text[p] == '\r'))
            {
                p++;
            }
            if ((p < Text.Length) && (Text[p] == '\n'))
            {
                start = lineStart;
                end = p + 1;
            }
        }
        Replace(start, end, string.Empty);
        return true;
    }

    /// <summary>The file content as bytes, with the byte order mark if the original had one.</summary>
    public byte[] ToBytes()
    {
        byte[] body = s_raw.GetBytes(Text);
        if (!HasBom)
        {
            return body;
        }
        byte[] result = new byte[body.Length + 3];
        s_utf8Bom.CopyTo(result, 0);
        body.CopyTo(result, 3);
        return result;
    }

    /// <summary>The whole text, decoded.</summary>
    public override string ToString() => DecodeRaw(Text);

    /// <summary>Saves to <see cref="FilePath"/>.</summary>
    public void Save()
    {
        if (FilePath is null)
        {
            throw new InvalidOperationException("The document has no file path.");
        }
        Save(FilePath);
    }

    /// <summary>
    /// Writes the file through a temporary file in the same folder, so a crash can't leave half a file. Retries for
    /// about two seconds while the file is locked (e.g. by the game).
    /// </summary>
    public void Save(string p_FilePath)
    {
        string fullPath = Path.GetFullPath(p_FilePath);
        AtomicFile.WriteAllBytes(fullPath, ToBytes());
        FilePath = fullPath;
        IsModified = false;
    }

    internal string DecodeRaw(string p_Raw) => m_valueEncoding.GetString(s_raw.GetBytes(p_Raw));

    internal (int Line, int Column) GetLineColumn(int p_Position)
    {
        int index = m_lineStarts.BinarySearch(p_Position);
        if (index < 0)
        {
            index = ~index - 1;
        }
        index = Math.Max(index, 0);
        return (index + 1, p_Position - m_lineStarts[index] + 1);
    }

    private static Encoding GetAnsiEncoding() => TextEncodings.Ansi;

    private static byte[] ReadAllBytesShared(string p_FilePath)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                using FileStream stream = new(p_FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using MemoryStream memory = new();
                stream.CopyTo(memory);
                return memory.ToArray();
            }
            catch (IOException) when ((attempt < 10) && File.Exists(p_FilePath))
            {
                Thread.Sleep(200);
            }
        }
    }

    private static void ValidateName(string p_Name)
    {
        foreach (char c in p_Name)
        {
            if (char.IsWhiteSpace(c) || (c is '{' or '}' or '=' or '\'' or ','))
            {
                throw new ArgumentException($"'{p_Name}' is not a valid config node name.", nameof(p_Name));
            }
        }
    }

    private static string BuildChain(ReadOnlySpan<string> p_Parts, string p_Value, string p_Indent, string p_Unit, string p_NewLine)
    {
        StringBuilder sb = new();
        string indent = p_Indent;
        List<string> closers = [];
        for (int i = 0; i < p_Parts.Length; i++)
        {
            _ = sb.Append(indent).Append(p_Parts[i]);
            if (i == (p_Parts.Length - 1))
            {
                _ = sb.Append(" = '").Append(p_Value).Append('\'').Append(p_NewLine);
            }
            else
            {
                _ = sb.Append(" {").Append(p_NewLine);
                closers.Add(indent);
                indent += p_Unit;
            }
        }
        for (int i = closers.Count - 1; i >= 0; i--)
        {
            _ = sb.Append(closers[i]).Append('}').Append(p_NewLine);
        }
        return sb.ToString();
    }

    private string[] RelativeParts(string p_Path)
    {
        string[] parts = SplitPath(p_Path);
        return ((parts.Length > 0) && string.Equals(parts[0], Root.Name, StringComparison.Ordinal)) ? parts[1..] : parts;
    }

    private string EncodeRaw(string p_Value) => s_raw.GetString(m_valueEncoding.GetBytes(p_Value));

    private void Replace(int p_Start, int p_End, string p_RawText)
    {
        string newText = string.Concat(Text.AsSpan(0, p_Start), p_RawText, Text.AsSpan(p_End));
        string oldText = Text;
        Text = newText;
        try
        {
            Root = Reparse();
        }
        catch (CfgParseException)
        {
            // Should never happen; keep the document valid.
            Text = oldText;
            Root = Reparse();
            throw;
        }
        IsModified = true;
    }

    private CfgNode Reparse()
    {
        m_lineStarts = [0];
        for (int i = 0; i < Text.Length; i++)
        {
            if (Text[i] == '\n')
            {
                m_lineStarts.Add(i + 1);
            }
        }
        return new CfgParser(this).ParseDocument();
    }

    private int GetLineStart(int p_Position) => (p_Position <= 0) ? 0 : Text.LastIndexOf('\n', p_Position - 1) + 1;

    private bool IsBlank(int p_Start, int p_End)
    {
        for (int i = p_Start; i < p_End; i++)
        {
            if (Text[i] is not (' ' or '\t'))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>The whitespace before the node name, or null if other text comes first on that line.</summary>
    private string? GetLineIndent(int p_NameStart)
    {
        int lineStart = GetLineStart(p_NameStart);
        return IsBlank(lineStart, p_NameStart) ? Text[lineStart..p_NameStart] : null;
    }

    private string DetectIndentUnit()
    {
        foreach (CfgNode child in Root.Children)
        {
            string? indent = GetLineIndent(child.NameStart);
            if (!string.IsNullOrEmpty(indent))
            {
                return indent;
            }
        }
        return "\t";
    }
}