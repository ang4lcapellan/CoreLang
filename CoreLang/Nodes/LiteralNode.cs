namespace CoreLang.Nodes
{
    public class LiteralNode : ExpressionNode
    {
        public object Value { get; }

        public LiteralNode(object value)
        {
            Value = value;
        }
    }
}
