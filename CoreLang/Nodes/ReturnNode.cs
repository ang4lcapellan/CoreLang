namespace CoreLang.Nodes
{
    public class ReturnNode : StatementNode
    {
        public ExpressionNode Value { get; }

        public ReturnNode(ExpressionNode value)
        {
            Value = value;
        }
    }
}
