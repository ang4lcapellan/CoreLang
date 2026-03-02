using CoreLang.Semantic.Scopes;
using CoreLang.Semantic.Types;

namespace CoreLang.Semantic.Symbols
{
    public class ClassSymbol : ISymbol
    {
        public string Name { get; }
        public Dictionary<string, ISymbol> Members { get; }
        public Scope Scope { get; set; }

        /// <summary>
        /// The type exposed via ISymbol is a TypeSymbol with the class name.
        /// </summary>
        public TypeSymbol Type { get; }

        public ClassSymbol(string name, Scope parentScope)
        {
            Name = name;
            Members = new Dictionary<string, ISymbol>();
            Scope = new Scope(parentScope);
            Type = new TypeSymbol(name);
        }
    }
}
