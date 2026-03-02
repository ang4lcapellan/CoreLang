using CoreLang.Semantic.Exceptions;
using CoreLang.Semantic.Symbols;

namespace CoreLang.Semantic.Scopes
{
    public class Scope
    {
        private readonly Dictionary<string, ISymbol> _symbols = new();
        public Scope? Parent { get; }

        public Scope(Scope? parent = null)
        {
            Parent = parent;
        }

        public void Define(ISymbol symbol, int line = 0, int column = 0)
        {
            if (_symbols.ContainsKey(symbol.Name))
                throw new SemanticException(
                    $"Symbol '{symbol.Name}' is already defined in this scope.", line, column);

            _symbols[symbol.Name] = symbol;
        }

        public ISymbol? Resolve(string name)
        {
            if (_symbols.TryGetValue(name, out var symbol))
                return symbol;

            return Parent?.Resolve(name);
        }
    }
}
