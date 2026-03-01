// Program.cs (CORREGIDO)
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using CoreLang.Nodes;

namespace CoreLang
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string[] testSuite = new string[]
            {
                // 1) Tipos / variables / literales
                "declare edad: i = 25;",
                "declare precio: f = 19.99;",
                "declare activo: b = true;",
                "declare nombre: s = \"CoreLang\";",

                // 2) Binarias / unarias
                "declare calc: i = (5 + 3) * 2;",
                "declare logica: b = not (true and false);",

                // 3) Arreglos
                "declare list: i[3] = [1, 2, 3];",
                "set list[0] = 5;",

                // 4) Objetos y member access (solo lectura, no asignación)
                @"
                object Persona {
                    declare nombre: s;
                }
                declare p: Persona? = null;
                show(p.nombre);
                ",

                // 5) Control de flujo
                @"
                check (edad > 18) {
                    show(""Es mayor"");
                } otherwise {
                    show(""Es menor"");
                }
                ",

                // ✅ CORREGIDO: tu gramática exige ';' antes de ')'
                @"
                loop(declare j: i = 0; j < 5; set j = j + 1; ) {
                    show(j);
                }
                ",

                @"
                repeat(activo == true) {
                    set activo = false;
                }
                ",

                // 6) Funciones
                @"
                func sumar(x: i, y: i): i {
                    gives x + y;
                }
                declare resultado: i = sumar(10, 5);
                show(resultado);
                ",

                // 7) Use + entry
                @"
                use System;

                entry func principal() : i {
                    gives 0;
                }
                "
            };

            for (int i = 0; i < testSuite.Length; i++)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\n================== TEST {i + 1} ==================");
                Console.ResetColor();
                Console.WriteLine(testSuite[i].Trim());
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("------------------ AST -------------------");
                Console.ResetColor();

                TestCode(testSuite[i]);
            }
        }

        static void TestCode(string code)
        {
            var inputStream = new AntlrInputStream(code);
            var lexer = new CoreLangLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new CoreLangParser(tokenStream);

            // ✅ Mostrar errores sintácticos
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new DiagnosticErrorListener());

            var tree = parser.program();
            var visitor = new AstBuilderVisitor();

            try
            {
                var ast = (ProgramNode)visitor.Visit(tree);
                if (ast != null) PrintAst(ast);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error construyendo AST:\n{ex}");
                Console.ResetColor();
            }
        }

        static void PrintAst(AstNode node, string indent = "")
        {
            Console.WriteLine($"{indent}- {node.GetType().Name} (L:{node.Line}, C:{node.Column})");

            var properties = node.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
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