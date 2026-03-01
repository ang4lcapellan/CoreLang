using System.Collections.Generic;
using Antlr4.Runtime.Misc;
using CoreLang.Nodes;

namespace CoreLang;

public sealed class AstBuilderVisitor
    : CoreLangParserBaseVisitor<AstNode>
{
    public override AstNode VisitProgram(
        [NotNull] CoreLangParser.ProgramContext ctx)
    {
        var items = new List<AstNode>();

        foreach (var item in ctx.topLevelItem())
        {
            var node = Visit(item);

            if (node != null)
                items.Add(node);
        }

        return new ProgramNode(items)
        {
            Line = ctx.Start.Line,
            Column = ctx.Start.Column
        };
    }
}