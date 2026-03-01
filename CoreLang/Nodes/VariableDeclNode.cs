namespace CoreLang.Nodes
{
    public class VariableDeclNode : StatementNode
    {
        public string Name { get; }
        public TypeNode Type { get; }
        public ExpressionNode? Initializer { get; }

        public VariableDeclNode(string name, TypeNode type, ExpressionNode? initializer = null)
        {
            Name = name;
            Type = type;
            Initializer = initializer;
        }
    }
}
