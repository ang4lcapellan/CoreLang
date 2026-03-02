using CoreLang.Semantic.Scopes;
using CoreLang.Semantic.Types;

namespace CoreLang.Semantic.Symbols
{
    public class MethodSymbol : ISymbol
    {
        public string Name { get; }
        public TypeSymbol ReturnType { get; }
        public List<VariableSymbol> Parameters { get; }
        public Scope Scope { get; set; }

        /// <summary>
        /// The type exposed via ISymbol is the return type.
        /// </summary>
        public TypeSymbol Type => ReturnType;

        public MethodSymbol(string name, TypeSymbol returnType, Scope parentScope)
        {
            Name = name;
            ReturnType = returnType;
            Parameters = new List<VariableSymbol>();
            Scope = new Scope(parentScope);
        }
    }
}
