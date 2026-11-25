namespace MIRAGE_Launcher.Models;

public enum ExeCheckStatus
{
    /// <summary>The hash is one of the accepted ones.</summary>
    Ok,

    /// <summary>The file has another hash: not the expected PEST-Control version.</summary>
    Mismatch,

    Missing,

    /// <summary>No hashes in Settings.json for this file.</summary>
    NotChecked,
}

public sealed record ExeCheckResult(string FileName, ExeCheckStatus Status, string? Hash);