// Program.cs (VERSIÓN FINAL - COMPILADOR REAL)

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using Antlr4.Runtime;
using CoreLang.Nodes;
using CoreLang.Semantic;
using CoreLang.Semantic.Exceptions;

namespace CoreLang
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("CoreLang Compiler");
            Console.ResetColor();

            if (args.Length == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Usage: dotnet run <file.core>");
                Console.ResetColor();
                return;
            }

            string filePath = Path.GetFullPath(args[0]);

            if (!File.Exists(filePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"File not found: {filePath}");
                Console.ResetColor();
                return;
            }

            string code = File.ReadAllText(filePath);
            Compile(code);
        }

        static void Compile(string code)
        {
            try
            {
                // 1️⃣ Lexer
                var inputStream = new AntlrInputStream(code);
                var lexer = new CoreLangLexer(inputStream);
                var tokenStream = new CommonTokenStream(lexer);

                // 2️⃣ Parser
                var parser = new CoreLangParser(tokenStream);
                var tree = parser.program();

                // 3️⃣ Análisis Semántico (requiere entry obligatorio)
                var analyzer = new SemanticAnalyzer();
                analyzer.Analyze(tree);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✔ Semantic analysis successful.");
                Console.ResetColor();

                // 4️⃣ Construcción del AST
                var visitor = new AstBuilderVisitor();
                var ast = (ProgramNode)visitor.Visit(tree);

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n--- AST ---");
                Console.ResetColor();

                PrintAst(ast);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n✔ Compilation successful.");
                Console.ResetColor();
            }
            catch (SemanticException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Unexpected error:");
                Console.WriteLine(ex);
                Console.ResetColor();
            }
        }

        static void PrintAst(AstNode node, string indent = "")
        {
            Console.WriteLine($"{indent}- {node.GetType().Name} (L:{node.Line}, C:{node.Column})");

            var properties = node.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.Name != "Line" && p.Name != "Column");

            foreach (var prop in properties)
            {
                var val = prop.GetValue(node);
                if (val == null) continue;

                if (val is AstNode childNode)
                {
                    PrintAst(childNode, indent + "  ");
                }
                else if (val is IEnumerable collection && !(val is string))
                {
                    foreach (var item in collection)
                    {
                        if (item is AstNode collectionNode)
                            PrintAst(collectionNode, indent + "  ");
                    }
                }
            }
        }
    }
}