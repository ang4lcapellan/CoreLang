namespace CoreLang.Nodes
{
    public class IdentifierNode : ExpressionNode
    {
        public string Name { get; }

        public IdentifierNode(string name)
        {
            Name = name;
        }
    }
}
