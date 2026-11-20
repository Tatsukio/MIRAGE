namespace MIRAGE.Shared.Cfg;

/// <summary>
/// Reads the tree format of Settings.cfg and the other ParaWorld config files:
/// <code>
/// Root {
///     Global {
///         Language = 'uk'
///     }
/// }
/// </code>
/// A node is a name, then optionally <c>= 'value'</c>, then optionally a block in braces. Whitespace and line
/// breaks between the parts don't matter, commas between nodes are ignored. Values can't contain <c>'</c>.
/// </summary>
internal sealed class CfgParser(CfgDocument p_Document)
{
    private readonly CfgDocument m_document = p_Document;
    private readonly string m_text = p_Document.Text;
    private int m_pos;

    private bool AtEnd => m_pos >= m_text.Length;

    public CfgNode ParseDocument()
    {
        SkipWhitespace();
        if (AtEnd)
        {
            throw Error("The file is empty");
        }
        CfgNode root = ParseNode(null);
        SkipWhitespace();
        return !AtEnd ? throw Error($"Unexpected '{m_text[m_pos]}' after the end of the '{root.Name}' node") : root;
    }

    private static bool IsWhitespace(char p_Char) => p_Char is ' ' or '\t' or '\r' or '\n' or '\f' or '\v' or '\0';

    private static bool IsNameChar(char p_Char) => !IsWhitespace(p_Char) && (p_Char is not ('{' or '}' or '=' or '\'' or ','));

    private CfgNode ParseNode(CfgNode? p_Parent)
    {
        int nameStart = m_pos;
        while (!AtEnd && IsNameChar(m_text[m_pos]))
        {
            m_pos++;
        }
        if (m_pos == nameStart)
        {
            throw AtEnd ? Error("Expected a node name, but the file ends here") : Error($"Expected a node name, got '{m_text[m_pos]}'");
        }

        CfgNode node = new(m_document, p_Parent, m_text[nameStart..m_pos], nameStart, m_pos);

        // Optional "= 'value'". Some script files leave out the '=' ("type 'CFrameWindow'") or the quotes ("pos_x = 100").
        int afterName = m_pos;
        SkipWhitespace();
        bool hasEquals = !AtEnd && (m_text[m_pos] == '=');
        if (hasEquals || (!AtEnd && (m_text[m_pos] == '\'')))
        {
            if (hasEquals)
            {
                m_pos++;
                SkipWhitespace();
            }
            ParseValue(node);
            afterName = m_pos;
            SkipWhitespace();
        }

        // Optional "{ children }".
        if (!AtEnd && (m_text[m_pos] == '{'))
        {
            node.OpenBrace = m_pos;
            m_pos++;
            while (true)
            {
                SkipWhitespace();
                if (AtEnd)
                {
                    m_pos = node.OpenBrace;
                    throw Error($"The block of '{node.Name}' has no closing '}}'");
                }
                char c = m_text[m_pos];
                if (c == '}')
                {
                    node.CloseBrace = m_pos;
                    m_pos++;
                    node.End = m_pos;
                    break;
                }
                if (c == ',')
                {
                    m_pos++;
                    continue;
                }
                node.AddChild(ParseNode(node));
            }
        }
        else
        {
            // Leave the whitespace after a leaf to the caller.
            m_pos = afterName;
        }
        return node;
    }

    private void ParseValue(CfgNode p_Node)
    {
        if (!AtEnd && (m_text[m_pos] == '\''))
        {
            int valueStart = m_pos + 1;
            int valueEnd = FindClosingQuote(valueStart);
            if (valueEnd < 0)
            {
                throw Error($"The value of '{p_Node.Name}' has no closing '");
            }
            p_Node.ValueStart = valueStart;
            p_Node.ValueEnd = valueEnd;
            p_Node.IsValueQuoted = true;
            m_pos = valueEnd + 1;
        }
        else
        {
            int valueStart = m_pos;
            while (!AtEnd && IsNameChar(m_text[m_pos]))
            {
                m_pos++;
            }
            if (m_pos == valueStart)
            {
                throw Error($"Expected a value after '=' of '{p_Node.Name}'");
            }
            p_Node.ValueStart = valueStart;
            p_Node.ValueEnd = m_pos;
        }
        p_Node.End = m_pos;
    }

    /// <summary>
    /// The quote that ends a value: on the same line, the first one followed by whitespace, a brace, a comma or the
    /// end, so that "Name = 'O'Brien'" reads as O'Brien. Otherwise the first quote; -1 if there is none.
    /// </summary>
    private int FindClosingQuote(int p_Start)
    {
        int first = m_text.IndexOf('\'', p_Start);
        int lineEnd = m_text.IndexOf('\n', p_Start);
        if (lineEnd < 0)
        {
            lineEnd = m_text.Length;
        }
        for (int i = first; (i >= 0) && (i < lineEnd); i = m_text.IndexOf('\'', i + 1))
        {
            int next = i + 1;
            if ((next >= m_text.Length) || IsWhitespace(m_text[next]) || (m_text[next] is '{' or '}' or ','))
            {
                return i;
            }
        }
        return first;
    }

    private void SkipWhitespace()
    {
        while (!AtEnd && IsWhitespace(m_text[m_pos]))
        {
            m_pos++;
        }
    }

    private CfgParseException Error(string p_Message)
    {
        (int line, int column) = m_document.GetLineColumn(m_pos);
        return new CfgParseException(p_Message, line, column);
    }
}