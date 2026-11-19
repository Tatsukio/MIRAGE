using System;

namespace MIRAGE.Shared.Cfg;

/// <summary>The text is not a valid config tree. <see cref="Line"/> and <see cref="Column"/> are 1-based.</summary>
public sealed class CfgParseException(string p_Message, int p_Line, int p_Column)
    : FormatException($"{p_Message} (line {p_Line}, column {p_Column})")
{
    public string Reason { get; } = p_Message;

    public int Line { get; } = p_Line;

    public int Column { get; } = p_Column;
}