using MIRAGE.Shared.Cfg;
using System;
using System.IO;

namespace CfgEditor;

/// <summary>The commands of CfgEditor.exe. The exit codes are the same as in the old Haskell version.</summary>
public sealed class CfgCommands(TextWriter p_Out, TextWriter p_Err, string p_DefaultFile)
{
    public const int ExitOk = 0;
    public const int ExitError = 1;
    public const int ExitParseError = 100;
    public const int ExitWrongArgumentCount = 101;
    public const int ExitWrongCommand = 102;
    public const int ExitNothingRemoved = 201;
    public const int ExitNotFound = 301;

    private readonly TextWriter m_out = p_Out;
    private readonly TextWriter m_err = p_Err;
    private readonly string m_defaultFile = p_DefaultFile;

    public static string Usage
        => """
        CfgEditor 2.0 - reads and changes ParaWorld config files (Settings.cfg and the same tree format).

          CfgEditor <file>                          check that the file can be read (parse)
          CfgEditor -g|--get <path> [file]          print a value
          CfgEditor -s|--set <path> <value> [file]  set a value (missing nodes are created)
          CfgEditor -r|--remove <path> [file]       remove a node and everything below it
          CfgEditor                                 interactive mode (type 'help')

        <path> is like Root/Global/Language. Without [file], Settings.cfg in
        %APPDATA%\SpieleEntwicklungsKombinat\Paraworld is used.

        Exit codes: 0 ok, 1 file error, 100 parse error, 101 wrong number of arguments,
        102 unknown command, 201 nothing to remove, 301 value not found.
        """;

    /// <summary>Runs the command line; returns the exit code.</summary>
    public int Run(string[] p_Args)
    {
        if (p_Args.Length == 0)
        {
            return ExitWrongCommand;
        }
        string command = p_Args[0];
        switch (command.ToLowerInvariant())
        {
            case "-h":
            case "--help":
            case "/?":
                m_out.WriteLine(Usage);
                return ExitOk;
            case "-g":
            case "--get":
                return p_Args.Length is 2 or 3 ? Get(p_Args[1], FileArg(p_Args, 2)) : WrongCount(command);
            case "-s":
            case "--set":
                return p_Args.Length is 3 or 4 ? Set(p_Args[1], p_Args[2], FileArg(p_Args, 3)) : WrongCount(command);
            case "-r":
            case "--remove":
                return p_Args.Length is 2 or 3 ? Remove(p_Args[1], FileArg(p_Args, 2)) : WrongCount(command);
            default:
                if (command.StartsWith('-'))
                {
                    m_err.WriteLine($"Unknown command '{command}'.");
                    m_err.WriteLine(Usage);
                    return ExitWrongCommand;
                }
                return p_Args.Length == 1 ? Check(command) : WrongCount(command);
        }
    }

    public int Check(string p_File)
    {
        if (!TryLoad(p_File, out CfgDocument? document, out int exitCode))
        {
            return exitCode;
        }
        m_out.WriteLine($"OK: {document!.FilePath}");
        return ExitOk;
    }

    public int Get(string p_Path, string p_File)
    {
        if (!TryLoad(p_File, out CfgDocument? document, out int exitCode))
        {
            return exitCode;
        }
        string? value = document!.Get(p_Path);
        if (value is null)
        {
            m_err.WriteLine($"'{p_Path}' not found in {document.FilePath}.");
            return ExitNotFound;
        }
        m_out.WriteLine(value);
        return ExitOk;
    }

    public int Set(string p_Path, string p_Value, string p_File)
    {
        if (!TryLoad(p_File, out CfgDocument? document, out int exitCode))
        {
            return exitCode;
        }
        try
        {
            if (document!.Set(p_Path, p_Value))
            {
                document.Save();
            }
            return ExitOk;
        }
        catch (ArgumentException ex)
        {
            m_err.WriteLine(ex.Message);
            return ExitWrongCommand;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            m_err.WriteLine($"Could not save {p_File}: {ex.Message}");
            return ExitError;
        }
    }

    public int Remove(string p_Path, string p_File)
    {
        if (!TryLoad(p_File, out CfgDocument? document, out int exitCode))
        {
            return exitCode;
        }
        try
        {
            if (!document!.Remove(p_Path))
            {
                m_err.WriteLine($"'{p_Path}' not found, nothing was removed.");
                return ExitNothingRemoved;
            }
            document.Save();
            return ExitOk;
        }
        catch (InvalidOperationException ex)
        {
            m_err.WriteLine(ex.Message);
            return ExitWrongCommand;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            m_err.WriteLine($"Could not save {p_File}: {ex.Message}");
            return ExitError;
        }
    }

    private string FileArg(string[] p_Args, int p_Index) => p_Args.Length > p_Index ? p_Args[p_Index] : m_defaultFile;

    private int WrongCount(string p_Command)
    {
        m_err.WriteLine($"Wrong number of arguments for '{p_Command}'.");
        m_err.WriteLine(Usage);
        return ExitWrongArgumentCount;
    }

    private bool TryLoad(string p_File, out CfgDocument? p_Document, out int p_ExitCode)
    {
        p_Document = null;
        if (!File.Exists(p_File))
        {
            m_err.WriteLine($"File not found: {p_File}");
            p_ExitCode = ExitError;
            return false;
        }
        try
        {
            p_Document = CfgDocument.Load(p_File);
            p_ExitCode = ExitOk;
            return true;
        }
        catch (CfgParseException ex)
        {
            m_err.WriteLine($"Parse error in {p_File}: {ex.Message}");
            p_ExitCode = ExitParseError;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            m_err.WriteLine($"Could not read {p_File}: {ex.Message}");
            p_ExitCode = ExitError;
        }
        return false;
    }
}