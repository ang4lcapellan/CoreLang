using System;
using Antlr4.Runtime;

class Program
{
    static void Main()
    {
        var code = @"
entry func Main():i {
  declare x:i = 1;
  set x = x + 1;
  check (x == 2) {
    show(x);
  } otherwise {
    show(0);
  }
  gives x;
}
";

        var input = new AntlrInputStream(code);
        var lexer = new CoreLangLexer(input);
        var tokens = new CommonTokenStream(lexer);
        var parser = new CoreLangParser(tokens);

        var tree = parser.program();

        Console.WriteLine(tree.ToStringTree(parser));
        Console.WriteLine("OK");
    }
}