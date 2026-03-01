namespace CoreLang.Nodes
{
    public class ArrayAccessNode : ExpressionNode
    {
        public string ArrayName { get; }
        public ExpressionNode Index { get; }

        public ArrayAccessNode(string arrayName, ExpressionNode index)
        {
            ArrayName = arrayName;
            Index = index;
        }
    }
}
