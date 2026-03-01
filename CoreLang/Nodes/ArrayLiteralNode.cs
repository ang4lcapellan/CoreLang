using System.Collections.Generic;

namespace CoreLang.Nodes
{
    public class ArrayLiteralNode : ExpressionNode
    {
        public List<ExpressionNode> Elements { get; } = new();

        public ArrayLiteralNode(IEnumerable<ExpressionNode> elements)
        {
            Elements.AddRange(elements);
        }
    }
}
