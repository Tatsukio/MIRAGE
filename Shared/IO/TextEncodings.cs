using System;
using System.Globalization;
using System.Text;

namespace MIRAGE.Shared.IO;

public static class TextEncodings
{
    /// <summary>The Windows ANSI code page of the system, which the game uses for its text files (1252 if unknown).
    /// The tests set it to 1252, so they give the same result on every system.</summary>
    public static Encoding Ansi { get; internal set; } = CreateAnsi();

    /// <summary>A Windows code page, e.g. 1251 (Cyrillic).</summary>
    public static Encoding GetCodePage(int p_CodePage)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(p_CodePage);
    }

    private static Encoding CreateAnsi()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        try
        {
            return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.ANSICodePage);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            return Encoding.GetEncoding(1252);
        }
    }
}
