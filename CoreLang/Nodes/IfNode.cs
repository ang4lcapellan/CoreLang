namespace CoreLang.Nodes
{
    public class IfNode : StatementNode
    {
        public ExpressionNode Condition { get; }
        public BlockNode TrueBlock { get; }
        public BlockNode? FalseBlock { get; }

        public IfNode(ExpressionNode condition, BlockNode trueBlock, BlockNode? falseBlock = null)
        {
            Condition = condition;
            TrueBlock = trueBlock;
            FalseBlock = falseBlock;
        }
    }
}
