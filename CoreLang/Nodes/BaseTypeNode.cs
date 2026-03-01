namespace CoreLang.Nodes
{
    public class BaseTypeNode : TypeNode
    {
        public string TypeName { get; }
        public bool IsNullable { get; }

        public BaseTypeNode(string typeName, bool isNullable = false)
        {
            TypeName = typeName;
            IsNullable = isNullable;
        }
    }
}
