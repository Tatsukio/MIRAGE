using System;
using System.IO;

namespace CfgEditor;

/// <summary>
/// The interactive mode (CfgEditor.exe without arguments). Commands, not case sensitive:
/// <c>get &lt;path&gt;</c>, <c>set &lt;path&gt; &lt;value&gt;</c>, <c>remove &lt;path&gt;</c>, <c>cd &lt;file&gt;</c>,
/// <c>pwd</c>, <c>help</c>, <c>q</c>. The old forms <c>Get(path)</c>, <c>Set(path,value)</c> and
/// <c>Remove(path)</c> work too.
/// </summary>
public sealed class InteractiveShell(TextReader p_In, TextWriter p_Out, TextWriter p_Err, string p_StartFile)
{
    private readonly TextReader m_in = p_In;
    private readonly TextWriter m_out = p_Out;
    private readonly CfgCommands m_commands = new(p_Out, p_Err, p_StartFile);
    private string m_file = p_StartFile;

    public int Run()
    {
        m_out.WriteLine("CfgEditor 2.0 interactive mode. Type 'help' for the commands.");
        m_out.WriteLine($"File: {m_file}");
        _ = m_commands.Check(m_file);
        while (true)
        {
            m_out.Write("> ");
            string? line = m_in.ReadLine();
            if (line is null)
            {
                return CfgCommands.ExitOk;
            }
            if (!Execute(line.Trim()))
            {
                return CfgCommands.ExitOk;
            }
        }
    }

    /// <summary>Runs one command; false means quit.</summary>
    public bool Execute(string p_Line)
    {
        if (p_Line.Length == 0)
        {
            return true;
        }
        (string command, string[] args) = Split(p_Line);
        switch (command)
        {
            case "q":
            case "quit":
            case "exit":
                return false;
            case "help":
            case "?":
                m_out.WriteLine("get <path> | set <path> <value> | remove <path> | cd <file> | pwd | q");
                m_out.WriteLine("Get(path) | Set(path,value) | Remove(path) also work.");
                break;
            case "pwd":
                m_out.WriteLine(m_file);
                break;
            case "cd":
                if ((args.Length == 1) && File.Exists(args[0]))
                {
                    m_file = Path.GetFullPath(args[0]);
                    _ = m_commands.Check(m_file);
                }
                else
                {
                    m_out.WriteLine("Not a valid file.");
                }
                break;
            case "get" when args.Length == 1:
                _ = m_commands.Get(args[0], m_file);
                break;
            case "set" when args.Length == 2:
                if (m_commands.Set(args[0], args[1], m_file) == CfgCommands.ExitOk)
                {
                    m_out.WriteLine("Done.");
                }
                break;
            case "remove" when args.Length == 1:
                if (m_commands.Remove(args[0], m_file) == CfgCommands.ExitOk)
                {
                    m_out.WriteLine("Done.");
                }
                break;
            default:
                m_out.WriteLine($"Not a valid command: {p_Line}");
                break;
        }
        return true;
    }

    /// <summary>"set a/b some value" or "Set(a/b,some value)" -> ("set", ["a/b", "some value"]).</summary>
    private static (string Command, string[] Args) Split(string p_Line)
    {
        int paren = p_Line.IndexOf('(');
        if ((paren > 0) && p_Line.EndsWith(')'))
        {
            string name = p_Line[..paren].Trim().ToLowerInvariant();
            string inner = p_Line[(paren + 1)..^1];
            int comma = inner.IndexOf(',');
            string[] parenArgs = (comma < 0) ? [inner.Trim()] : [inner[..comma].Trim(), inner[(comma + 1)..].Trim()];
            return (name, parenArgs);
        }

        string[] words = p_Line.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string command = words[0].ToLowerInvariant();
        return command switch
        {
            "set" when words.Length == 3 => (command, [words[1], words[2]]),
            "cd" => (command, words.Length > 1 ? [p_Line[2..].Trim().Trim('"')] : []),
            _ => (command, words[1..]),
        };
    }
}