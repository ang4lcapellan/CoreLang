using CoreLang.Semantic.Types;

namespace CoreLang.Semantic.Symbols
{
    public interface ISymbol
    {
        string Name { get; }
        TypeSymbol Type { get; }
    }
}
