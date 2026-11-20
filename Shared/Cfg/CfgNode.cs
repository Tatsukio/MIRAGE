using System;
using System.Collections.Generic;

namespace MIRAGE.Shared.Cfg;

/// <summary>
/// One node of a ParaWorld config tree: <c>Name</c>, <c>Name = 'Value'</c>, <c>Name { ... }</c> or
/// <c>Name = 'Value' { ... }</c>. A node only describes the parsed text; change the tree through
/// <see cref="CfgDocument"/>.
/// </summary>
public sealed class CfgNode
{
    private readonly List<CfgNode> m_children = [];

    internal CfgNode(CfgDocument p_Document, CfgNode? p_Parent, string p_Name, int p_NameStart, int p_NameEnd)
    {
        Document = p_Document;
        Parent = p_Parent;
        Name = p_Name;
        NameStart = p_NameStart;
        NameEnd = p_NameEnd;
        End = p_NameEnd;
    }

    public CfgDocument Document { get; }

    public CfgNode? Parent { get; }

    public string Name { get; }

    /// <summary>The value, or null when the node has no <c>= '...'</c> part.</summary>
    public string? Value => HasValue ? Document.DecodeRaw(RawValue) : null;

    public bool HasValue => ValueStart >= 0;

    public bool HasBlock => OpenBrace >= 0;

    public IReadOnlyList<CfgNode> Children => m_children;

    /// <summary>The path from the root, e.g. <c>Root/Global/Language</c>.</summary>
    public string Path => Parent is null ? Name : $"{Parent.Path}/{Name}";

    internal string RawValue => HasValue ? Document.Text[ValueStart..ValueEnd] : string.Empty;

    internal int NameStart { get; }

    internal int NameEnd { get; }

    /// <summary>First character inside the quotes, or -1.</summary>
    internal int ValueStart { get; set; } = -1;

    /// <summary>The closing quote, or the end of a value without quotes.</summary>
    internal int ValueEnd { get; set; } = -1;

    internal bool IsValueQuoted { get; set; }

    internal int OpenBrace { get; set; } = -1;

    internal int CloseBrace { get; set; } = -1;

    /// <summary>The position after the last character of the node.</summary>
    internal int End { get; set; }

    public CfgNode? FindChild(string p_Name)
    {
        foreach (CfgNode child in m_children)
        {
            if (string.Equals(child.Name, p_Name, StringComparison.Ordinal))
            {
                return child;
            }
        }
        return null;
    }

    public override string ToString() => HasValue ? $"{Path} = '{Value}'" : Path;

    internal void AddChild(CfgNode p_Child) => m_children.Add(p_Child);
}