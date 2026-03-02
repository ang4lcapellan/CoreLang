namespace CoreLang.Semantic.Exceptions
{
    public class SemanticException : Exception
    {
        public int Line { get; }
        public int Column { get; }

        public SemanticException(string message, int line, int column)
            : base($"[Semantic Error at line {line}, column {column}] {message}")
        {
            Line = line;
            Column = column;
        }
    }
}
