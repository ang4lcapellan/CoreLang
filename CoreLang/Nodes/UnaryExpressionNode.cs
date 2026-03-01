namespace CoreLang.Nodes
{
    public class UnaryExpressionNode : ExpressionNode
    {
        public string Operator { get; }
        public ExpressionNode Operand { get; }

        public UnaryExpressionNode(string op, ExpressionNode operand)
        {
            Operator = op;
            Operand = operand;
        }
    }
}
