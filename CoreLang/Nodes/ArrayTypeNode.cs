namespace CoreLang.Nodes
{
    public class ArrayTypeNode : TypeNode
    {
        public TypeNode ElementType { get; }
        public int Size { get; }
        public bool IsNullable { get; }

        public ArrayTypeNode(TypeNode elementType, int size, bool isNullable = false)
        {
            ElementType = elementType;
            Size = size;
            IsNullable = isNullable;
        }
    }
}
