using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
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
    {
        return BuildFunction(context.IDENT(), context.paramListOpt(), context.typeRef(), context.block(), false, context.Start);
    }

    public override AstNode VisitEntryFuncDef([NotNull] CoreLangParser.EntryFuncDefContext context)
    {
        return BuildFunction(context.IDENT(), context.paramListOpt(), context.typeRef(), context.block(), true, context.Start);
    }

    private FunctionNode BuildFunction(
        Antlr4.Runtime.Tree.ITerminalNode ident, 
        CoreLangParser.ParamListOptContext paramCtx, 
        CoreLangParser.TypeRefContext typeRef, 
        CoreLangParser.BlockContext blockCtx, 
        bool isEntry, 
        IToken startToken)
    {
        var name = ident?.GetText() ?? string.Empty;
        
        TypeNode returnType = null;
        if (typeRef != null)
        {
            returnType = (TypeNode)VisitTypeRef(typeRef);
        }

        var block = (BlockNode)Visit(blockCtx);
        
        var parameters = new List<ParameterNode>();
        if (paramCtx != null && paramCtx.param() != null)
        {
            foreach (var p in paramCtx.param())
            {
                var pName = p.IDENT()?.GetText() ?? string.Empty;
                
                if (p.typeRef() != null)
                {
                    var pType = (TypeNode)VisitTypeRef(p.typeRef());
                    parameters.Add(new ParameterNode(pName, pType)
                    {
                        Line = p.Start?.Line ?? 0,
                        Column = p.Start?.Column ?? 0
                    });
                }
            }
        }

        return new FunctionNode(name, parameters, returnType, block, isEntry)
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
        bool isNullable = context.nullOpt() != null && !string.IsNullOrEmpty(context.nullOpt().GetText());

        if (typeCoreCtx.baseType() != null)
        {
            return new BaseTypeNode(typeCoreCtx.baseType().GetText(), isNullable) 
            { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0 };
        }
        else if (typeCoreCtx.classType() != null)
        {
            return new ClassTypeNode(typeCoreCtx.classType().GetText(), isNullable) 
            { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0 };
        }
        else if (typeCoreCtx.arrayType() != null)
        {
            var arrType = typeCoreCtx.arrayType();
            TypeNode elementNode;
            if (arrType.baseType() != null)
                elementNode = new BaseTypeNode(arrType.baseType().GetText(), isNullable: false);
            else
                elementNode = new ClassTypeNode(arrType.classType().GetText(), isNullable: false);
            
            int size = int.Parse(arrType.INT_LIT().GetText());
            return new ArrayTypeNode(elementNode, size, isNullable) 
            { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0 };
        }

        return null;
    }

    // --- 3. STATEMENTS ---
    public override AstNode VisitVarDecl([NotNull] CoreLangParser.VarDeclContext context)
    {
        string name = context.IDENT()?.GetText() ?? string.Empty;
        var type = (TypeNode)Visit(context.typeRef());
        
        ExpressionNode initializer = null;
        if (context.initOpt() != null && context.initOpt().expr() != null)
        {
            initializer = (ExpressionNode)Visit(context.initOpt().expr());
        }

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
        
        BlockNode falseBlock = null;
        if (context.otherwiseOpt() != null && context.otherwiseOpt().block() != null)
        {
            falseBlock = (BlockNode)Visit(context.otherwiseOpt().block());
        }

        return new IfNode(condition, trueBlock, falseBlock)
        {
            Line = context.Start?.Line ?? 0,
            Column = context.Start?.Column ?? 0
        };
    }

    public override AstNode VisitLoopStmt([NotNull] CoreLangParser.LoopStmtContext context)
    {
        StatementNode init = context.loopInit() != null ? Visit(context.loopInit()) as StatementNode : null;
        var condition = (ExpressionNode)Visit(context.expr());
        AstNode action = context.loopAction() != null ? Visit(context.loopAction()) : null;
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

    // Handles an standalone expression used as a statement (e.g. show(1);)
    public override AstNode VisitSimpleStmt([NotNull] CoreLangParser.SimpleStmtContext context)
    {
        if (context.expr() != null)
        {
            return new ExpressionStatementNode((ExpressionNode)Visit(context.expr()))
            {
                Line = context.Start?.Line ?? 0,
                Column = context.Start?.Column ?? 0
            };
        }
        return base.VisitSimpleStmt(context); // default visit for varDecl, setStmt, givesStmt inside simpleStmt
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
        {
            return Visit(context.primary());
        }
        else if (context.unaryExpr() != null)
        {
            string op = context.NOT() != null ? "not" : "-";
            var operand = (ExpressionNode)Visit(context.unaryExpr());
            return new UnaryExpressionNode(op, operand) 
            { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0 };
        }
        
        return null;
    }

    public override AstNode VisitPrimary([NotNull] CoreLangParser.PrimaryContext context)
    {
        if (context.literal() != null) return Visit(context.literal());
        if (context.arrayLit() != null) return Visit(context.arrayLit());
        if (context.IDENT() != null) return new IdentifierNode(context.IDENT().GetText()) { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0 };
        
        if (context.lenCall() != null) return Visit(context.lenCall());
        if (context.askCall() != null) return Visit(context.askCall());
        if (context.convertCall() != null) return Visit(context.convertCall());
        if (context.showCall() != null) return Visit(context.showCall());
        
        if (context.callExpr() != null) return Visit(context.callExpr());
        if (context.memberAccess() != null) return Visit(context.memberAccess());
        if (context.indexAccess() != null) return Visit(context.indexAccess());
        if (context.LPAREN() != null && context.expr() != null) return Visit(context.expr()); 
        
        return null; // Fallback
    }

    // --- CALLS & ACCESS ---
    public override AstNode VisitCallExpr([NotNull] CoreLangParser.CallExprContext context)
    {
        string funcName = context.IDENT().GetText();
        var arguments = new List<ExpressionNode>();

        if (context.argListOpt() != null)
        {
            foreach (var exprCtx in context.argListOpt().expr())
            {
                arguments.Add((ExpressionNode)Visit(exprCtx));
            }
        }
        return new CallNode(funcName, arguments) { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0 };
    }

    public override AstNode VisitShowCall([NotNull] CoreLangParser.ShowCallContext context)
        => new CallNode("show", new List<ExpressionNode> { (ExpressionNode)Visit(context.expr()) }) { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0 };

    public override AstNode VisitLenCall([NotNull] CoreLangParser.LenCallContext context)
        => new CallNode("len", new List<ExpressionNode> { new IdentifierNode(context.IDENT().GetText()) }) { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0 };

    public override AstNode VisitAskCall([NotNull] CoreLangParser.AskCallContext context)
        => new CallNode("ask", new List<ExpressionNode> { (ExpressionNode)Visit(context.lvalue()) }) { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0 };

    public override AstNode VisitConvertCall([NotNull] CoreLangParser.ConvertCallContext context)
        => new CallNode(context.convertName().GetText(), new List<ExpressionNode> { (ExpressionNode)Visit(context.expr()) }) { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0 };

    public override AstNode VisitMemberAccess([NotNull] CoreLangParser.MemberAccessContext context)
    {
        string objectName = context.IDENT(0).GetText();
        string memberName = context.IDENT(1).GetText();
        return new MemberAccessNode(objectName, memberName) { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0 };
    }

    public override AstNode VisitIndexAccess([NotNull] CoreLangParser.IndexAccessContext context)
    {
        string arrayName = context.IDENT().GetText();
        var index = (ExpressionNode)Visit(context.expr());
        return new ArrayAccessNode(arrayName, index) { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0 };
    }

    // --- LVALUE HANDLING ---
    public override AstNode VisitLvalue([NotNull] CoreLangParser.LvalueContext context)
    {
        // For array index assign "ident[expr]"
        if (context.LBRACK() != null)
        {
            string arrayName = context.IDENT().GetText();
            var index = (ExpressionNode)Visit(context.expr());
            return new ArrayAccessNode(arrayName, index) { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0 };
        }
        
        // As defined in the grammar `lvalue : IDENT | IDENT LBRACK expr RBRACK`
        // But in Test 9 we have `p.nombre`, however the grammar doesn't allow member access in lvalue! 
        // Wait, looking closely at Test 9...
        // `set p.nombre = "Juan";` This IS NOT an lvalue as per grammar.
        // Wait, the grammar says:
        // `setStmt : SET lvalue ASSIGN expr ;`
        // `lvalue : IDENT | IDENT LBRACK expr RBRACK`
        
        string ident = context.IDENT()?.GetText() ?? string.Empty;
        return new IdentifierNode(ident) { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0 };
    }

    // --- LITERALS ---
    public override AstNode VisitLiteral([NotNull] CoreLangParser.LiteralContext context)
    {
        var lineInfo = new { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0 };

        if (context.INT_LIT() != null) return new LiteralNode(int.Parse(context.INT_LIT().GetText())) { Line = lineInfo.Line, Column = lineInfo.Column };
        if (context.FLOAT_LIT() != null) return new LiteralNode(float.Parse(context.FLOAT_LIT().GetText(), System.Globalization.CultureInfo.InvariantCulture)) { Line = lineInfo.Line, Column = lineInfo.Column };
        if (context.STRING_LIT() != null) 
        {
            string s = context.STRING_LIT().GetText(); 
            return new LiteralNode(s.Substring(1, s.Length - 2)) { Line = lineInfo.Line, Column = lineInfo.Column }; // Remove quotes
        }
        if (context.TRUE() != null) return new LiteralNode(true) { Line = lineInfo.Line, Column = lineInfo.Column };
        if (context.FALSE() != null) return new LiteralNode(false) { Line = lineInfo.Line, Column = lineInfo.Column };
        if (context.NULL() != null) return new LiteralNode(null) { Line = lineInfo.Line, Column = lineInfo.Column };

        return null;
    }

    public override AstNode VisitArrayLit([NotNull] CoreLangParser.ArrayLitContext context)
    {
        var elements = new List<ExpressionNode>();
        if (context.elementsOpt() != null)
        {
            foreach (var exprCtx in context.elementsOpt().expr())
            {
                elements.Add((ExpressionNode)Visit(exprCtx));
            }
        }
        return new ArrayLiteralNode(elements) { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0 };
    }


    // --- HELPERS PARA BINARY EXPRESSIONS ---
    // ANTLR parses left associative chains like A + B + C as a list of exprs: [A, B, C] and ops: [+, +]
    private AstNode EvaluateBinaryChain<TContext, TOpContext>(TContext[] contexts, TOpContext[] ops)
        where TContext : ParserRuleContext
        where TOpContext : ParserRuleContext
    {
        var current = (ExpressionNode)Visit(contexts[0]);
        for (int i = 0; i < ops.Length; i++)
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

    private AstNode EvaluateBinaryChain<TContext>(TContext[] contexts, string op)
        where TContext : ParserRuleContext
    {
        var current = (ExpressionNode)Visit(contexts[0]);
        for (int i = 1; i < contexts.Length; i++)
        {
            var right = (ExpressionNode)Visit(contexts[i]);
            current = new BinaryExpressionNode(current, op, right)
            {
                // we associate the token location with the right term context
                Line = contexts[i].Start?.Line ?? 0,
                Column = contexts[i].Start?.Column ?? 0
            };
        }
        return current;
    }

}
