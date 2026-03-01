using System;
using System.Collections.Generic;
using System.Text;

namespace CoreLang.Nodes
{
public class ProgramNode : AstNode
{
    public List<AstNode> Items { get; } = new();

    public ProgramNode(IEnumerable<AstNode> items)
    {
        Items.AddRange(items);
    }
}
}
