namespace CoreLang.Nodes
{
    public class RepeatNode : StatementNode
    {
        public ExpressionNode Condition { get; }
        public BlockNode Body { get; }

        public RepeatNode(ExpressionNode condition, BlockNode body)
        {
            Condition = condition;
            Body = body;
        }
    }
}
