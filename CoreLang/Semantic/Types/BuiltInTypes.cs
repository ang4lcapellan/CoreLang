namespace CoreLang.Semantic.Types
{
    public static class BuiltInTypes
    {
        public static readonly TypeSymbol Int = new("i");
        public static readonly TypeSymbol Float = new("f");
        public static readonly TypeSymbol Bool = new("b");
        public static readonly TypeSymbol String = new("s");

        /// <summary>
        /// Sentinel type for the null literal. Not a real type — only used for assignability checks.
        /// </summary>
        public static readonly TypeSymbol Null = new("null");
    }
}
