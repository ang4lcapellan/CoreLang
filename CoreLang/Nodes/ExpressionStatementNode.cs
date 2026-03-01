namespace CoreLang.Nodes
{
    public class ExpressionStatementNode : StatementNode
    {
        public ExpressionNode Expression { get; }

        public ExpressionStatementNode(ExpressionNode expression)
        {
            Expression = expression;
        }
    }
}
