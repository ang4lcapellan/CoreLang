namespace CoreLang.Nodes
{
    public class AssignmentNode : StatementNode
    {
        // Using an ExpressionNode for the target allows for array indexing (e.g., set arr[0] = 5)
        public ExpressionNode Target { get; }
        public ExpressionNode Value { get; }

        public AssignmentNode(ExpressionNode target, ExpressionNode value)
        {
            Target = target;
            Value = value;
        }
    }
}
