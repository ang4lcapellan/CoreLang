// AstBuilderVisitor.cs (CORREGIDO)
using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using CoreLang.Nodes;

namespace CoreLang;

public sealed class AstBuilderVisitor : CoreLangParserBaseVisitor<AstNode>
{
    // --- 1. PROGRAM STRUCTURE ---
    public override AstNode VisitProgram([NotNull] CoreLangParser.ProgramContext context)
    {
        var items = new List<AstNode>();

        foreach (var item in context.topLevelItem())
        {
            var node = Visit(item);
            if (node != null) items.Add(node);
        }

        return new ProgramNode(items)
        {
            Line = context.Start?.Line ?? 0,
            Column = context.Start?.Column ?? 0
        };
    }

    public override AstNode VisitUseStmt([NotNull] CoreLangParser.UseStmtContext context)
    {
        var idText = context.IDENT()?.GetText() ?? string.Empty;
        return new UseNode(idText)
        {
            Line = context.Start?.Line ?? 0,
            Column = context.Start?.Column ?? 0
        };
    }

    public override AstNode VisitClassDef([NotNull] CoreLangParser.ClassDefContext context)
    {
        var idText = context.IDENT()?.GetText() ?? string.Empty;
        var members = new List<AstNode>();

        var block = context.classBlock();
        if (block != null)
        {
            foreach (var member in block.classMember())
            {
                var memberNode = Visit(member);
                if (memberNode != null) members.Add(memberNode);
            }
        }

        return new ClassNode(idText, members)
        {
            Line = context.Start?.Line ?? 0,
            Column = context.Start?.Column ?? 0
        };
    }

    public override AstNode VisitFuncDef([NotNull] CoreLangParser.FuncDefContext context)
        => BuildFunction(context.IDENT(), context.paramListOpt(), context.typeRef(), context.block(), false, context.Start);

    public override AstNode VisitEntryFuncDef([NotNull] CoreLangParser.EntryFuncDefContext context)
        => BuildFunction(context.IDENT(), context.paramListOpt(), context.typeRef(), context.block(), true, context.Start);

    private FunctionNode BuildFunction(
        ITerminalNode ident,
        CoreLangParser.ParamListOptContext paramCtx,
        CoreLangParser.TypeRefContext typeRef,
        CoreLangParser.BlockContext blockCtx,
        bool isEntry,
        IToken startToken)
    {
        var name = ident?.GetText() ?? string.Empty;

        // En tu gramática SIEMPRE hay ": typeRef", así que esto no debería ser null,
        // pero lo dejamos seguro.
        TypeNode? returnType = typeRef != null ? (TypeNode)VisitTypeRef(typeRef) : null;

        var block = (BlockNode)Visit(blockCtx);

        var parameters = new List<ParameterNode>();
        if (paramCtx != null)
        {
            foreach (var p in paramCtx.param())
            {
                var pName = p.IDENT()?.GetText() ?? string.Empty;
                var pType = (TypeNode)VisitTypeRef(p.typeRef());

                parameters.Add(new ParameterNode(pName, pType)
                {
                    Line = p.Start?.Line ?? 0,
                    Column = p.Start?.Column ?? 0
                });
            }
        }

        return new FunctionNode(name, parameters, returnType!, block, isEntry)
        {
            Line = startToken?.Line ?? 0,
            Column = startToken?.Column ?? 0
        };
    }

    public override AstNode VisitBlock([NotNull] CoreLangParser.BlockContext context)
    {
        var stmts = new List<StatementNode>();
        foreach (var stmtCtx in context.stmt())
        {
            var stmt = Visit(stmtCtx) as StatementNode;
            if (stmt != null) stmts.Add(stmt);
        }

        return new BlockNode(stmts)
        {
            Line = context.Start?.Line ?? 0,
            Column = context.Start?.Column ?? 0
        };
    }

    // --- 2. TYPES ---
    public override AstNode VisitTypeRef([NotNull] CoreLangParser.TypeRefContext context)
    {
        var typeCoreCtx = context.typeCore();

        // Nullable si existe '?' en nullOpt
        bool isNullable = context.nullOpt()?.QMARK() != null;

        // baseType
        if (typeCoreCtx.baseType() != null)
        {
            return new BaseTypeNode(typeCoreCtx.baseType().GetText(), isNullable)
            {
                Line = context.Start?.Line ?? 0,
                Column = context.Start?.Column ?? 0
            };
        }

        // classType
        if (typeCoreCtx.classType() != null)
        {
            return new ClassTypeNode(typeCoreCtx.classType().GetText(), isNullable)
            {
                Line = context.Start?.Line ?? 0,
                Column = context.Start?.Column ?? 0
            };
        }

        // arrayType
        if (typeCoreCtx.arrayType() != null)
        {
            var arrType = typeCoreCtx.arrayType();

            TypeNode elementNode;
            if (arrType.baseType() != null)
            {
                elementNode = new BaseTypeNode(arrType.baseType().GetText(), isNullable: false)
                {
                    Line = arrType.Start?.Line ?? 0,
                    Column = arrType.Start?.Column ?? 0
                };
            }
            else
            {
                elementNode = new ClassTypeNode(arrType.classType().GetText(), isNullable: false)
                {
                    Line = arrType.Start?.Line ?? 0,
                    Column = arrType.Start?.Column ?? 0
                };
            }

            int size = int.Parse(arrType.INT_LIT().GetText());

            return new ArrayTypeNode(elementNode, size, isNullable)
            {
                Line = context.Start?.Line ?? 0,
                Column = context.Start?.Column ?? 0
            };
        }

        // En vez de throw (para que no reviente), devuelve null o un tipo "unknown"
        return null;
    }

    // --- 3. STATEMENTS ---
    public override AstNode VisitVarDecl([NotNull] CoreLangParser.VarDeclContext context)
    {
        string name = context.IDENT()?.GetText() ?? string.Empty;
        var type = (TypeNode)Visit(context.typeRef());

        ExpressionNode? initializer = null;
        if (context.initOpt()?.expr() != null)
            initializer = (ExpressionNode)Visit(context.initOpt().expr());

        return new VariableDeclNode(name, type, initializer)
        {
            Line = context.Start?.Line ?? 0,
            Column = context.Start?.Column ?? 0
        };
    }

    public override AstNode VisitSetStmt([NotNull] CoreLangParser.SetStmtContext context)
    {
        var target = (ExpressionNode)Visit(context.lvalue());
        var value = (ExpressionNode)Visit(context.expr());

        return new AssignmentNode(target, value)
        {
            Line = context.Start?.Line ?? 0,
            Column = context.Start?.Column ?? 0
        };
    }

    public override AstNode VisitGivesStmt([NotNull] CoreLangParser.GivesStmtContext context)
    {
        var value = (ExpressionNode)Visit(context.expr());
        return new ReturnNode(value)
        {
            Line = context.Start?.Line ?? 0,
            Column = context.Start?.Column ?? 0
        };
    }

    public override AstNode VisitCheckStmt([NotNull] CoreLangParser.CheckStmtContext context)
    {
        var condition = (ExpressionNode)Visit(context.expr());
        var trueBlock = (BlockNode)Visit(context.block());

        BlockNode? falseBlock = null;
        if (context.otherwiseOpt()?.block() != null)
            falseBlock = (BlockNode)Visit(context.otherwiseOpt().block());

        return new IfNode(condition, trueBlock, falseBlock)
        {
            Line = context.Start?.Line ?? 0,
            Column = context.Start?.Column ?? 0
        };
    }

    public override AstNode VisitLoopStmt([NotNull] CoreLangParser.LoopStmtContext context)
    {
        // loopInit puede ser empty, varDecl o setStmt
        StatementNode? init = context.loopInit() != null ? Visit(context.loopInit()) as StatementNode : null;

        var condition = (ExpressionNode)Visit(context.expr());

        // loopAction puede ser empty, setStmt o expr
        AstNode? action = context.loopAction() != null ? Visit(context.loopAction()) : null;

        var body = (BlockNode)Visit(context.block());

        return new LoopNode(init, condition, action, body)
        {
            Line = context.Start?.Line ?? 0,
            Column = context.Start?.Column ?? 0
        };
    }

    public override AstNode VisitRepeatStmt([NotNull] CoreLangParser.RepeatStmtContext context)
    {
        var condition = (ExpressionNode)Visit(context.expr());
        var body = (BlockNode)Visit(context.block());

        return new RepeatNode(condition, body)
        {
            Line = context.Start?.Line ?? 0,
            Column = context.Start?.Column ?? 0
        };
    }

    public override AstNode VisitSimpleStmt([NotNull] CoreLangParser.SimpleStmtContext context)
    {
        // expr como statement: expr ;
        if (context.expr() != null)
        {
            return new ExpressionStatementNode((ExpressionNode)Visit(context.expr()))
            {
                Line = context.Start?.Line ?? 0,
                Column = context.Start?.Column ?? 0
            };
        }

        return base.VisitSimpleStmt(context);
    }

    // --- 4. EXPRESSIONS ---
    public override AstNode VisitOrExpr([NotNull] CoreLangParser.OrExprContext context)
        => EvaluateBinaryChain(context.andExpr(), "or");

    public override AstNode VisitAndExpr([NotNull] CoreLangParser.AndExprContext context)
        => EvaluateBinaryChain(context.eqExpr(), "and");

    public override AstNode VisitEqExpr([NotNull] CoreLangParser.EqExprContext context)
        => EvaluateBinaryChain(context.relExpr(), context.eqOp());

    public override AstNode VisitRelExpr([NotNull] CoreLangParser.RelExprContext context)
        => EvaluateBinaryChain(context.addExpr(), context.relOp());

    public override AstNode VisitAddExpr([NotNull] CoreLangParser.AddExprContext context)
        => EvaluateBinaryChain(context.mulExpr(), context.addOp());

    public override AstNode VisitMulExpr([NotNull] CoreLangParser.MulExprContext context)
        => EvaluateBinaryChain(context.unaryExpr(), context.mulOp());

    public override AstNode VisitUnaryExpr([NotNull] CoreLangParser.UnaryExprContext context)
    {
        if (context.primary() != null)
            return Visit(context.primary());

        if (context.unaryExpr() != null)
        {
            string op = context.NOT() != null ? "not" : "-";
            var operand = (ExpressionNode)Visit(context.unaryExpr());
            return new UnaryExpressionNode(op, operand)
            { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0 };
        }

        throw new InvalidOperationException("unaryExpr inválida.");
    }

    public override AstNode VisitPrimary([NotNull] CoreLangParser.PrimaryContext context)
    {
        if (context.literal() != null) return Visit(context.literal());
        if (context.arrayLit() != null) return Visit(context.arrayLit());

        if (context.lenCall() != null) return Visit(context.lenCall());
        if (context.askCall() != null) return Visit(context.askCall());
        if (context.convertCall() != null) return Visit(context.convertCall());
        if (context.showCall() != null) return Visit(context.showCall());

        if (context.callExpr() != null) return Visit(context.callExpr());
        if (context.memberAccess() != null) return Visit(context.memberAccess());
        if (context.indexAccess() != null) return Visit(context.indexAccess());

        if (context.IDENT() != null)
            return new IdentifierNode(context.IDENT().GetText())
            { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0 };

        if (context.LPAREN() != null && context.expr() != null)
            return Visit(context.expr());

        throw new InvalidOperationException("primary inválido.");
    }

    // --- CALLS & ACCESS ---
    public override AstNode VisitCallExpr([NotNull] CoreLangParser.CallExprContext context)
    {
        var idents = context.children.Where(c => c is ITerminalNode t && t.Symbol.Type == CoreLangLexer.IDENT).Select(c => c.GetText()).ToList();
        string? objectName = null;
        string funcName;

        if (idents.Count == 2)
        {
            objectName = idents[0];
            funcName = idents[1];
        }
        else
        {
            funcName = idents[0];
        }
        
        var arguments = new List<ExpressionNode>();

        if (context.argListOpt() != null)
        {
            foreach (var exprCtx in context.argListOpt().expr())
            {
                arguments.Add((ExpressionNode)Visit(exprCtx));
            }
        }
        
        if (objectName != null) return new CallNode(objectName, funcName, arguments);
        else return new CallNode(funcName, arguments);
    }

    public override AstNode VisitShowCall([NotNull] CoreLangParser.ShowCallContext context)
        => new CallNode("show", new List<ExpressionNode> { (ExpressionNode)Visit(context.expr()) })
        { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0 };

    public override AstNode VisitLenCall([NotNull] CoreLangParser.LenCallContext context)
        => new CallNode("len", new List<ExpressionNode> { new IdentifierNode(context.IDENT().GetText()) })
        { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0 };

    public override AstNode VisitAskCall([NotNull] CoreLangParser.AskCallContext context)
        => new CallNode("ask", new List<ExpressionNode> { (ExpressionNode)Visit(context.lvalue()) })
        { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0 };

    public override AstNode VisitConvertCall([NotNull] CoreLangParser.ConvertCallContext context)
        => new CallNode(context.convertName().GetText(), new List<ExpressionNode> { (ExpressionNode)Visit(context.expr()) })
        { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0 };

    public override AstNode VisitMemberAccess([NotNull] CoreLangParser.MemberAccessContext context)
    {
        string objectName = context.IDENT(0).GetText();
        string memberName = context.IDENT(1).GetText();
        return new MemberAccessNode(objectName, memberName)
        { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0 };
    }

    public override AstNode VisitIndexAccess([NotNull] CoreLangParser.IndexAccessContext context)
    {
        string arrayName = context.IDENT().GetText();
        var index = (ExpressionNode)Visit(context.expr());
        return new ArrayAccessNode(arrayName, index)
        { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0 };
    }

    public override AstNode VisitLvalue([NotNull] CoreLangParser.LvalueContext context)
    {
        if (context.LBRACK() != null)
        {
            string arrayName = context.IDENT().GetText();
            var index = (ExpressionNode)Visit(context.expr());
            return new ArrayAccessNode(arrayName, index)
            { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0 };
        }

        string ident = context.IDENT()?.GetText() ?? string.Empty;
        return new IdentifierNode(ident)
        { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0 };
    }

    // --- LITERALS ---
    public override AstNode VisitLiteral([NotNull] CoreLangParser.LiteralContext context)
    {
        int line = context.Start?.Line ?? 0;
        int col = context.Start?.Column ?? 0;

        if (context.INT_LIT() != null)
            return new LiteralNode(int.Parse(context.INT_LIT().GetText())) { Line = line, Column = col };

        if (context.FLOAT_LIT() != null)
            return new LiteralNode(float.Parse(context.FLOAT_LIT().GetText(), System.Globalization.CultureInfo.InvariantCulture))
            { Line = line, Column = col };

        if (context.STRING_LIT() != null)
        {
            string s = context.STRING_LIT().GetText();
            return new LiteralNode(s.Substring(1, s.Length - 2)) { Line = line, Column = col };
        }

        if (context.TRUE() != null) return new LiteralNode(true) { Line = line, Column = col };
        if (context.FALSE() != null) return new LiteralNode(false) { Line = line, Column = col };
        if (context.NULL() != null) return new LiteralNode(null) { Line = line, Column = col };

        throw new InvalidOperationException("literal inválido.");
    }

    public override AstNode VisitArrayLit([NotNull] CoreLangParser.ArrayLitContext context)
    {
        var elements = new List<ExpressionNode>();

        var opt = context.elementsOpt();
        if (opt != null)
        {
            foreach (var exprCtx in opt.expr())
                elements.Add((ExpressionNode)Visit(exprCtx));
        }

        return new ArrayLiteralNode(elements)
        {
            Line = context.Start?.Line ?? 0,
            Column = context.Start?.Column ?? 0
        };
    }
    // --- HELPERS (IList en vez de arrays) ---
    private AstNode EvaluateBinaryChain<TContext, TOpContext>(IList<TContext> contexts, IList<TOpContext> ops)
        where TContext : ParserRuleContext
        where TOpContext : ParserRuleContext
    {
        var current = (ExpressionNode)Visit(contexts[0]);
        for (int i = 0; i < ops.Count; i++)
        {
            var right = (ExpressionNode)Visit(contexts[i + 1]);
            string op = ops[i].GetText();
            current = new BinaryExpressionNode(current, op, right)
            {
                Line = ops[i].Start?.Line ?? 0,
                Column = ops[i].Start?.Column ?? 0
            };
        }
        return current;
    }

    private AstNode EvaluateBinaryChain<TContext>(IList<TContext> contexts, string op)
        where TContext : ParserRuleContext
    {
        var current = (ExpressionNode)Visit(contexts[0]);
        for (int i = 1; i < contexts.Count; i++)
        {
            var right = (ExpressionNode)Visit(contexts[i]);
            current = new BinaryExpressionNode(current, op, right)
            {
                Line = contexts[i].Start?.Line ?? 0,
                Column = contexts[i].Start?.Column ?? 0
            };
        }
        return current;
    }
    // --- WRAPPERS IMPORTANTES (para no perder nodos por el ';') ---

    public override AstNode VisitTopLevelItem([NotNull] CoreLangParser.TopLevelItemContext context)
    {
        // Devuelve el primer hijo que produzca un AstNode
        return VisitChildrenFirst(context);
    }

    public override AstNode VisitClassMember([NotNull] CoreLangParser.ClassMemberContext context)
    {
        return VisitChildrenFirst(context);
    }

    public override AstNode VisitStmt([NotNull] CoreLangParser.StmtContext context)
    {
        return VisitChildrenFirst(context);
    }

    public override AstNode VisitStmtSemi([NotNull] CoreLangParser.StmtSemiContext context)
    {
        // stmtSemi : simpleStmt SEMI ;
        // Si no lo haces, el visitor devuelve el resultado del SEMI (null).
        return Visit(context.simpleStmt());
    }

    // Helper: visita hijos y retorna el primer AstNode no nulo
    private AstNode VisitChildrenFirst(ParserRuleContext ctx)
    {
        for (int i = 0; i < ctx.ChildCount; i++)
        {
            var child = ctx.GetChild(i);
            var result = child.Accept(this);
            if (result != null) return result;
        }
        return null;
    }
}