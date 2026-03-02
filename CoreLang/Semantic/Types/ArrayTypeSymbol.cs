namespace CoreLang.Semantic.Types
{
    public class ArrayTypeSymbol : TypeSymbol
    {
        public TypeSymbol ElementType { get; }

        public ArrayTypeSymbol(TypeSymbol elementType, bool isNullable = false)
            : base($"{elementType.Name}[]", isNullable)
        {
            ElementType = elementType;
        }

        public override bool IsArray() => true;

        public override string ToString() => IsNullable ? $"{ElementType.Name}[]?" : $"{ElementType.Name}[]";
    }
}
