namespace CoreLang.Nodes
{
    public class LoopNode : StatementNode
    {
        // loopInit in your grammar can be empty, varDecl, or setStmt
        public StatementNode? Init { get; } 
        public ExpressionNode Condition { get; }
        // loopAction in your grammar can be empty, setStmt, or expr
        public AstNode? Action { get; } 
        public BlockNode Body { get; }

        public LoopNode(StatementNode? init, ExpressionNode condition, AstNode? action, BlockNode body)
        {
            Init = init;
            Condition = condition;
            Action = action;
            Body = body;
        }
    }
}
