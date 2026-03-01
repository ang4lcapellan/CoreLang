using CoreLang.Nodes;
using System.Collections.Generic;


namespace CoreLang.Nodes;

public class BlockNode : StatementNode
{
    public List<StatementNode> Statements { get; } = new();

    public BlockNode(IEnumerable<StatementNode> statements)
    {
        Statements.AddRange(statements);
    }
}