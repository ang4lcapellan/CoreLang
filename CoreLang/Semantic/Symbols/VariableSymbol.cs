using CoreLang.Semantic.Types;

namespace CoreLang.Semantic.Symbols
{
    public class VariableSymbol : ISymbol
    {
        public string Name { get; }
        public TypeSymbol Type { get; }
        public bool IsParameter { get; }
        public bool IsField { get; }

        public VariableSymbol(string name, TypeSymbol type, bool isParameter = false, bool isField = false)
        {
            Name = name;
            Type = type;
            IsParameter = isParameter;
            IsField = isField;
        }
    }
}
