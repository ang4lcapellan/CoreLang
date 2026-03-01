using System;
using Antlr4.Runtime;
using CoreLang.Nodes;

namespace CoreLang
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Código de prueba mínimo válido según tu gramática
            string code = @"
                declare x:i = 5 + 2;
            ";

            // 1) Input stream
            var inputStream = new AntlrInputStream(code);

            // 2) Lexer
            var lexer = new CoreLangLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);

            // 3) Parser
            var parser = new CoreLangParser(tokenStream);

            // Opcional: mostrar errores detallados
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new DiagnosticErrorListener());

            // 4) Parse tree
            var tree = parser.program();

            // 5) Visitor → AST
            var visitor = new AstBuilderVisitor();
            var ast = (ProgramNode)visitor.Visit(tree);

            // 6) Imprimir AST
            Console.WriteLine("=== AST ===");
            PrintAst(ast);
        }

        // Método simple para imprimir el AST
        static void PrintAst(AstNode? node, int indent = 0)
        {
            if (node == null) return;

            string padding = new string(' ', indent * 2);
            Console.WriteLine($"{padding}{node.GetType().Name}");

            switch (node)
            {
                case ProgramNode program:
                    foreach (var item in program.Items)
                        PrintAst(item, indent + 1);
                    break;

                    // Más nodos se agregan aquí luego
            }
        }
    }
}