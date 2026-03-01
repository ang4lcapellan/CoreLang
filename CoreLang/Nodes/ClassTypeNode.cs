namespace CoreLang.Nodes
{
    public class ClassTypeNode : TypeNode
    {
        public string ClassName { get; }
        public bool IsNullable { get; }

        public ClassTypeNode(string className, bool isNullable = false)
        {
            ClassName = className;
            IsNullable = isNullable;
        }
    }
}
