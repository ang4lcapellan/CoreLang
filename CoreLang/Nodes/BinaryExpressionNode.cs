namespace CoreLang.Nodes
{
    public class BinaryExpressionNode : ExpressionNode
    {
        public ExpressionNode Left { get; }
        public string Operator { get; }
        public ExpressionNode Right { get; }

        public BinaryExpressionNode(ExpressionNode left, string op, ExpressionNode right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }
    }
}
