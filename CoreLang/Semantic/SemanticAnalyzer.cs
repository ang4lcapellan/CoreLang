using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using CoreLang.Semantic.Exceptions;
using CoreLang.Semantic.Scopes;
using CoreLang.Semantic.Symbols;
using CoreLang.Semantic.Types;

namespace CoreLang.Semantic
{
    public class SemanticAnalyzer : CoreLangParserBaseVisitor<TypeSymbol>
    {
        private Scope _currentScope = null!;
        private MethodSymbol? _currentMethod;
        private ClassSymbol? _currentClass;
        private bool _hasEntry;

        // Phase flag: true during phase 1 (registration), false during phase 2 (validation)
        private bool _registering;

        // ───────────────────────── PUBLIC API ─────────────────────────

        public void Analyze(CoreLangParser.ProgramContext tree)
        {
            _currentScope = new GlobalScope();
            _hasEntry = false;

            // Phase 1: Registration only
            _registering = true;
            Visit(tree);

            // Phase 2: Validation only
            _registering = false;
            _currentScope = new GlobalScope();
            _hasEntry = false;
            Phase2_ReRegisterSymbols(tree);
            Visit(tree);
        }

        /// <summary>
        /// In Phase 2 we need the symbols available again, so we quickly re-register them
        /// without any validation — reusing Phase 1 logic.
        /// </summary>
        private void Phase2_ReRegisterSymbols(CoreLangParser.ProgramContext tree)
        {
            bool saved = _registering;
            _registering = true;
            Visit(tree);
            _registering = saved;
        }

        // ───────────────────────── PROGRAM ─────────────────────────

        public override TypeSymbol VisitProgram([NotNull] CoreLangParser.ProgramContext context)
        {
            foreach (var item in context.topLevelItem())
                Visit(item);

            if (!_registering && !_hasEntry)
                throw new SemanticException("Program must have exactly one 'entry' function.",
                    context.Start.Line, context.Start.Column);

            return null!;
        }

        // ───────────────────────── TOP LEVEL DISPATCH ─────────────────────────

        public override TypeSymbol VisitTopLevelItem([NotNull] CoreLangParser.TopLevelItemContext context)
        {
            if (context.classDef() != null) return Visit(context.classDef());
            if (context.entryFuncDef() != null) return Visit(context.entryFuncDef());
            if (context.funcDef() != null) return Visit(context.funcDef());
            if (context.stmtSemi() != null) return Visit(context.stmtSemi());
            if (context.compoundStmt() != null) return Visit(context.compoundStmt());
            if (context.useStmt() != null) return Visit(context.useStmt());
            return null!;
        }

        // ───────────────────────── USE ─────────────────────────

        public override TypeSymbol VisitUseStmt([NotNull] CoreLangParser.UseStmtContext context)
        {
            // use statements are accepted syntactically but not validated semantically
            return null!;
        }

        // ───────────────────────── CLASS DEFINITION ─────────────────────────

        public override TypeSymbol VisitClassDef([NotNull] CoreLangParser.ClassDefContext context)
        {
            var name = context.IDENT().GetText();

            if (_registering)
            {
                var classSymbol = new ClassSymbol(name, _currentScope);
                _currentScope.Define(classSymbol, context.Start.Line, context.Start.Column);

                var prevScope = _currentScope;
                var prevClass = _currentClass;
                _currentScope = classSymbol.Scope;
                _currentClass = classSymbol;

                foreach (var member in context.classBlock().classMember())
                    Visit(member);

                _currentScope = prevScope;
                _currentClass = prevClass;
            }
            else
            {
                // Phase 2: validate class members
                var symbol = _currentScope.Resolve(name) as ClassSymbol;
                if (symbol == null)
                    throw new SemanticException($"Class '{name}' not found.",
                        context.Start.Line, context.Start.Column);

                var prevScope = _currentScope;
                var prevClass = _currentClass;
                _currentScope = symbol.Scope;
                _currentClass = symbol;

                foreach (var member in context.classBlock().classMember())
                    Visit(member);

                _currentScope = prevScope;
                _currentClass = prevClass;
            }

            return null!;
        }

        public override TypeSymbol VisitClassMember([NotNull] CoreLangParser.ClassMemberContext context)
        {
            if (context.entryFuncDef() != null) return Visit(context.entryFuncDef());
            if (context.varDecl() != null) return Visit(context.varDecl());
            if (context.methodDef() != null) return Visit(context.methodDef());
            if (context.stmtSemi() != null) return Visit(context.stmtSemi());
            if (context.compoundStmt() != null) return Visit(context.compoundStmt());
            return null!;
        }

        // ───────────────────────── ENTRY FUNCTION ─────────────────────────

        public override TypeSymbol VisitEntryFuncDef([NotNull] CoreLangParser.EntryFuncDefContext context)
        {
            var name = context.IDENT().GetText();
            var returnType = ResolveTypeRef(context.typeRef());

            if (_registering)
            {
                if (_hasEntry)
                    throw new SemanticException("Only one 'entry' function is allowed.",
                        context.Start.Line, context.Start.Column);
                _hasEntry = true;

                if (returnType != BuiltInTypes.Int)
                    throw new SemanticException("'entry' function must return type 'i'.",
                        context.Start.Line, context.Start.Column);

                var method = new MethodSymbol(name, returnType, _currentScope);
                RegisterParams(method, context.paramListOpt());
                _currentScope.Define(method, context.Start.Line, context.Start.Column);
                
                if (_currentClass != null)
                    _currentClass.Members[name] = method;
            }
            else
            {
                _hasEntry = true;
                var method = _currentScope.Resolve(name) as MethodSymbol;
                if (method == null)
                    throw new SemanticException($"Entry function '{name}' not found.",
                        context.Start.Line, context.Start.Column);

                var prevMethod = _currentMethod;
                var prevScope = _currentScope;
                _currentMethod = method;
                _currentScope = method.Scope;

                VisitBlockWithScope(context.block());
                ValidateGivesAtEnd(context.block(), method.ReturnType);

                _currentMethod = prevMethod;
                _currentScope = prevScope;
            }

            return null!;
        }

        // ───────────────────────── FUNCTION DEFINITION ─────────────────────────

        public override TypeSymbol VisitFuncDef([NotNull] CoreLangParser.FuncDefContext context)
        {
            var name = context.IDENT().GetText();
            var returnType = ResolveTypeRef(context.typeRef());

            if (_registering)
            {
                var method = new MethodSymbol(name, returnType, _currentScope);
                RegisterParams(method, context.paramListOpt());
                _currentScope.Define(method, context.Start.Line, context.Start.Column);
            }
            else
            {
                var method = _currentScope.Resolve(name) as MethodSymbol;
                if (method == null)
                    throw new SemanticException($"Function '{name}' not found.",
                        context.Start.Line, context.Start.Column);

                var prevMethod = _currentMethod;
                var prevScope = _currentScope;
                _currentMethod = method;
                _currentScope = method.Scope;

                VisitBlockWithScope(context.block());
                ValidateGivesAtEnd(context.block(), method.ReturnType);

                _currentMethod = prevMethod;
                _currentScope = prevScope;
            }

            return null!;
        }

        // ───────────────────────── METHOD DEFINITION (inside class) ─────────────────────────

        public override TypeSymbol VisitMethodDef([NotNull] CoreLangParser.MethodDefContext context)
        {
            var name = context.IDENT().GetText();
            var returnType = ResolveTypeRef(context.typeRef());

            if (_registering)
            {
                var method = new MethodSymbol(name, returnType, _currentScope);
                RegisterParams(method, context.paramListOpt());
                _currentScope.Define(method, context.Start.Line, context.Start.Column);

                if (_currentClass != null)
                    _currentClass.Members[name] = method;
            }
            else
            {
                var method = _currentScope.Resolve(name) as MethodSymbol;
                if (method == null)
                    throw new SemanticException($"Method '{name}' not found.",
                        context.Start.Line, context.Start.Column);

                var prevMethod = _currentMethod;
                var prevScope = _currentScope;
                _currentMethod = method;
                _currentScope = method.Scope;

                VisitBlockWithScope(context.block());
                ValidateGivesAtEnd(context.block(), method.ReturnType);

                _currentMethod = prevMethod;
                _currentScope = prevScope;
            }

            return null!;
        }

        // ───────────────────────── BLOCKS ─────────────────────────

        public override TypeSymbol VisitBlock([NotNull] CoreLangParser.BlockContext context)
        {
            if (_registering) return null!;

            foreach (var stmt in context.stmt())
                Visit(stmt);

            return null!;
        }

        /// <summary>
        /// Visits a block creating a new child scope and restoring on exit.
        /// </summary>
        private void VisitBlockWithScope(CoreLangParser.BlockContext context)
        {
            var prevScope = _currentScope;
            _currentScope = new Scope(_currentScope);

            foreach (var stmt in context.stmt())
                Visit(stmt);

            _currentScope = prevScope;
        }

        // ───────────────────────── STATEMENTS ─────────────────────────

        public override TypeSymbol VisitStmt([NotNull] CoreLangParser.StmtContext context)
        {
            if (context.stmtSemi() != null) return Visit(context.stmtSemi());
            if (context.compoundStmt() != null) return Visit(context.compoundStmt());
            return null!;
        }

        public override TypeSymbol VisitStmtSemi([NotNull] CoreLangParser.StmtSemiContext context)
        {
            return Visit(context.simpleStmt());
        }

        public override TypeSymbol VisitSimpleStmt([NotNull] CoreLangParser.SimpleStmtContext context)
        {
            if (context.varDecl() != null) return Visit(context.varDecl());
            if (context.setStmt() != null) return Visit(context.setStmt());
            if (context.givesStmt() != null) return Visit(context.givesStmt());
            if (context.expr() != null) return Visit(context.expr());
            return null!;
        }

        public override TypeSymbol VisitCompoundStmt([NotNull] CoreLangParser.CompoundStmtContext context)
        {
            if (context.checkStmt() != null) return Visit(context.checkStmt());
            if (context.loopStmt() != null) return Visit(context.loopStmt());
            if (context.repeatStmt() != null) return Visit(context.repeatStmt());
            return null!;
        }

        // ───────────────────────── VARIABLE DECLARATION ─────────────────────────

        public override TypeSymbol VisitVarDecl([NotNull] CoreLangParser.VarDeclContext context)
        {
            var name = context.IDENT().GetText();
            var type = ResolveTypeRef(context.typeRef());

            if (_registering)
            {
                bool isField = _currentClass != null && _currentMethod == null;
                var varSymbol = new VariableSymbol(name, type, isField: isField);
                _currentScope.Define(varSymbol, context.Start.Line, context.Start.Column);

                if (isField && _currentClass != null)
                    _currentClass.Members[name] = varSymbol;
            }
            else
            {
                // Re-define in current scope for Phase 2 local variables
                bool isField = _currentClass != null && _currentMethod == null;
                var existing = _currentScope.Resolve(name);
                if (existing == null)
                {
                    var varSymbol = new VariableSymbol(name, type, isField: isField);
                    _currentScope.Define(varSymbol, context.Start.Line, context.Start.Column);
                }

                // Validate initializer if present
                var initOpt = context.initOpt();
                if (initOpt != null && initOpt.expr() != null)
                {
                    var exprType = Visit(initOpt.expr());
                    if (exprType != null && !type.IsAssignableFrom(exprType))
                        throw new SemanticException(
                            $"Cannot assign type '{exprType}' to variable '{name}' of type '{type}'.",
                            context.Start.Line, context.Start.Column);
                }
            }

            return null!;
        }

        // ───────────────────────── SET STATEMENT ─────────────────────────

        public override TypeSymbol VisitSetStmt([NotNull] CoreLangParser.SetStmtContext context)
        {
            if (_registering) return null!;

            var lvalueCtx = context.lvalue();
            var varName = lvalueCtx.IDENT().GetText();
            var symbol = _currentScope.Resolve(varName);

            if (symbol == null)
                throw new SemanticException($"Variable '{varName}' is not declared.",
                    context.Start.Line, context.Start.Column);

            var targetType = symbol.Type;

            // Array index assignment: set arr[i] = expr
            if (lvalueCtx.LBRACK() != null)
            {
                if (!targetType.IsArray())
                    throw new SemanticException($"Variable '{varName}' is not an array.",
                        context.Start.Line, context.Start.Column);

                var indexType = Visit(lvalueCtx.expr());
                if (indexType != BuiltInTypes.Int)
                    throw new SemanticException("Array index must be of type 'i'.",
                        lvalueCtx.Start.Line, lvalueCtx.Start.Column);

                targetType = ((ArrayTypeSymbol)targetType).ElementType;
            }

            var exprType = Visit(context.expr());
            if (exprType != null && !targetType.IsAssignableFrom(exprType))
                throw new SemanticException(
                    $"Cannot assign type '{exprType}' to '{varName}' of type '{targetType}'.",
                    context.Start.Line, context.Start.Column);

            return null!;
        }

        // ───────────────────────── GIVES STATEMENT ─────────────────────────

        public override TypeSymbol VisitGivesStmt([NotNull] CoreLangParser.GivesStmtContext context)
        {
            if (_registering) return null!;

            if (_currentMethod == null)
                throw new SemanticException("'gives' statement found outside of a function.",
                    context.Start.Line, context.Start.Column);

            var exprType = Visit(context.expr());
            if (exprType != null && !_currentMethod.ReturnType.IsAssignableFrom(exprType))
                throw new SemanticException(
                    $"Return type mismatch: expected '{_currentMethod.ReturnType}', got '{exprType}'.",
                    context.Start.Line, context.Start.Column);

            return null!;
        }

        // ───────────────────────── CHECK (if/else) ─────────────────────────

        public override TypeSymbol VisitCheckStmt([NotNull] CoreLangParser.CheckStmtContext context)
        {
            if (_registering) return null!;

            var condType = Visit(context.expr());
            if (condType != null && !condType.IsBoolean())
                throw new SemanticException("Condition in 'check' must be of type 'b'.",
                    context.Start.Line, context.Start.Column);

            VisitBlockWithScope(context.block());

            var otherwiseOpt = context.otherwiseOpt();
            if (otherwiseOpt != null && otherwiseOpt.block() != null)
                VisitBlockWithScope(otherwiseOpt.block());

            return null!;
        }

        // ───────────────────────── LOOP (for) ─────────────────────────

        public override TypeSymbol VisitLoopStmt([NotNull] CoreLangParser.LoopStmtContext context)
        {
            if (_registering) return null!;

            // Loop creates its own scope for init variable
            var prevScope = _currentScope;
            _currentScope = new Scope(_currentScope);

            var loopInit = context.loopInit();
            if (loopInit.varDecl() != null) Visit(loopInit.varDecl());
            else if (loopInit.setStmt() != null) Visit(loopInit.setStmt());

            var condType = Visit(context.expr());
            if (condType != null && !condType.IsBoolean())
                throw new SemanticException("Loop condition must be of type 'b'.",
                    context.Start.Line, context.Start.Column);

            var loopAction = context.loopAction();
            if (loopAction.setStmt() != null) Visit(loopAction.setStmt());
            else if (loopAction.expr() != null) Visit(loopAction.expr());

            VisitBlockWithScope(context.block());

            _currentScope = prevScope;

            return null!;
        }

        // ───────────────────────── REPEAT (while) ─────────────────────────

        public override TypeSymbol VisitRepeatStmt([NotNull] CoreLangParser.RepeatStmtContext context)
        {
            if (_registering) return null!;

            var condType = Visit(context.expr());
            if (condType != null && !condType.IsBoolean())
                throw new SemanticException("Condition in 'repeat' must be of type 'b'.",
                    context.Start.Line, context.Start.Column);

            VisitBlockWithScope(context.block());

            return null!;
        }

        // ───────────────────────── EXPRESSIONS ─────────────────────────

        public override TypeSymbol VisitExpr([NotNull] CoreLangParser.ExprContext context)
        {
            return Visit(context.orExpr());
        }

        public override TypeSymbol VisitOrExpr([NotNull] CoreLangParser.OrExprContext context)
        {
            var andExprs = context.andExpr();
            var left = Visit(andExprs[0]);

            for (int i = 1; i < andExprs.Length; i++)
            {
                if (left != null && !left.IsBoolean())
                    throw new SemanticException("Operand of 'or' must be of type 'b'.",
                        context.Start.Line, context.Start.Column);

                var right = Visit(andExprs[i]);
                if (right != null && !right.IsBoolean())
                    throw new SemanticException("Operand of 'or' must be of type 'b'.",
                        andExprs[i].Start.Line, andExprs[i].Start.Column);

                left = BuiltInTypes.Bool;
            }

            return left;
        }

        public override TypeSymbol VisitAndExpr([NotNull] CoreLangParser.AndExprContext context)
        {
            var eqExprs = context.eqExpr();
            var left = Visit(eqExprs[0]);

            for (int i = 1; i < eqExprs.Length; i++)
            {
                if (left != null && !left.IsBoolean())
                    throw new SemanticException("Operand of 'and' must be of type 'b'.",
                        context.Start.Line, context.Start.Column);

                var right = Visit(eqExprs[i]);
                if (right != null && !right.IsBoolean())
                    throw new SemanticException("Operand of 'and' must be of type 'b'.",
                        eqExprs[i].Start.Line, eqExprs[i].Start.Column);

                left = BuiltInTypes.Bool;
            }

            return left;
        }

        public override TypeSymbol VisitEqExpr([NotNull] CoreLangParser.EqExprContext context)
        {
            var relExprs = context.relExpr();
            var left = Visit(relExprs[0]);

            for (int i = 1; i < relExprs.Length; i++)
            {
                var right = Visit(relExprs[i]);

                // Equality operators require type compatibility
                if (left != null && right != null &&
                    !left.IsAssignableFrom(right) && !right.IsAssignableFrom(left))
                {
                    throw new SemanticException(
                        $"Cannot compare type '{left}' with '{right}' using equality operator.",
                        context.eqOp(i - 1).Start.Line, context.eqOp(i - 1).Start.Column);
                }

                left = BuiltInTypes.Bool;
            }

            return left;
        }

        public override TypeSymbol VisitRelExpr([NotNull] CoreLangParser.RelExprContext context)
        {
            var addExprs = context.addExpr();
            var left = Visit(addExprs[0]);

            for (int i = 1; i < addExprs.Length; i++)
            {
                if (left != null && !left.IsNumeric())
                    throw new SemanticException("Relational operator requires numeric operands.",
                        context.Start.Line, context.Start.Column);

                var right = Visit(addExprs[i]);
                if (right != null && !right.IsNumeric())
                    throw new SemanticException("Relational operator requires numeric operands.",
                        addExprs[i].Start.Line, addExprs[i].Start.Column);

                left = BuiltInTypes.Bool;
            }

            return left;
        }

        public override TypeSymbol VisitAddExpr([NotNull] CoreLangParser.AddExprContext context)
        {
            var mulExprs = context.mulExpr();
            var left = Visit(mulExprs[0]);

            for (int i = 1; i < mulExprs.Length; i++)
            {
                if (left != null && !left.IsNumeric())
                    throw new SemanticException("Arithmetic operator requires numeric operands.",
                        context.Start.Line, context.Start.Column);

                var right = Visit(mulExprs[i]);
                if (right != null && !right.IsNumeric())
                    throw new SemanticException("Arithmetic operator requires numeric operands.",
                        mulExprs[i].Start.Line, mulExprs[i].Start.Column);

                // Promote to float if either is float
                left = (left == BuiltInTypes.Float || right == BuiltInTypes.Float)
                    ? BuiltInTypes.Float
                    : BuiltInTypes.Int;
            }

            return left;
        }

        public override TypeSymbol VisitMulExpr([NotNull] CoreLangParser.MulExprContext context)
        {
            var unaryExprs = context.unaryExpr();
            var left = Visit(unaryExprs[0]);

            for (int i = 1; i < unaryExprs.Length; i++)
            {
                if (left != null && !left.IsNumeric())
                    throw new SemanticException("Arithmetic operator requires numeric operands.",
                        context.Start.Line, context.Start.Column);

                var right = Visit(unaryExprs[i]);
                if (right != null && !right.IsNumeric())
                    throw new SemanticException("Arithmetic operator requires numeric operands.",
                        unaryExprs[i].Start.Line, unaryExprs[i].Start.Column);

                left = (left == BuiltInTypes.Float || right == BuiltInTypes.Float)
                    ? BuiltInTypes.Float
                    : BuiltInTypes.Int;
            }

            return left;
        }

        public override TypeSymbol VisitUnaryExpr([NotNull] CoreLangParser.UnaryExprContext context)
        {
            if (context.NOT() != null)
            {
                var operand = Visit(context.unaryExpr());
                if (operand != null && !operand.IsBoolean())
                    throw new SemanticException("Operand of 'not' must be of type 'b'.",
                        context.Start.Line, context.Start.Column);
                return BuiltInTypes.Bool;
            }

            if (context.MINUS() != null)
            {
                var operand = Visit(context.unaryExpr());
                if (operand != null && !operand.IsNumeric())
                    throw new SemanticException("Unary minus requires a numeric operand.",
                        context.Start.Line, context.Start.Column);
                return operand;
            }

            return Visit(context.primary());
        }

        // ───────────────────────── PRIMARY ─────────────────────────

        public override TypeSymbol VisitPrimary([NotNull] CoreLangParser.PrimaryContext context)
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

            // Parenthesized expression
            if (context.expr() != null) return Visit(context.expr());

            // Bare identifier
            if (context.IDENT() != null)
            {
                var name = context.IDENT().GetText();
                var symbol = _currentScope.Resolve(name);
                if (symbol == null)
                    throw new SemanticException($"Variable '{name}' is not declared.",
                        context.Start.Line, context.Start.Column);
                return symbol.Type;
            }

            return null!;
        }

        // ───────────────────────── LITERALS ─────────────────────────

        public override TypeSymbol VisitLiteral([NotNull] CoreLangParser.LiteralContext context)
        {
            if (context.INT_LIT() != null) return BuiltInTypes.Int;
            if (context.FLOAT_LIT() != null) return BuiltInTypes.Float;
            if (context.STRING_LIT() != null) return BuiltInTypes.String;
            if (context.TRUE() != null) return BuiltInTypes.Bool;
            if (context.FALSE() != null) return BuiltInTypes.Bool;
            if (context.NULL() != null) return BuiltInTypes.Null;
            return null!;
        }

        // ───────────────────────── ARRAY LITERAL ─────────────────────────

        public override TypeSymbol VisitArrayLit([NotNull] CoreLangParser.ArrayLitContext context)
        {
            var elementsOpt = context.elementsOpt();
            if (elementsOpt == null || elementsOpt.expr() == null || elementsOpt.expr().Length == 0)
                return null!;

            var exprs = elementsOpt.expr();
            var firstType = Visit(exprs[0]);

            for (int i = 1; i < exprs.Length; i++)
            {
                var elemType = Visit(exprs[i]);
                if (elemType != null && firstType != null && !firstType.IsAssignableFrom(elemType))
                    throw new SemanticException(
                        $"Array element type mismatch: expected '{firstType}', got '{elemType}'.",
                        exprs[i].Start.Line, exprs[i].Start.Column);
            }

            return firstType != null ? new ArrayTypeSymbol(firstType) : null!;
        }

        // ───────────────────────── CALLS ─────────────────────────

        public override TypeSymbol VisitCallExpr([NotNull] CoreLangParser.CallExprContext context)
        {
            var idents = context.children.Where(c => c is ITerminalNode t && t.Symbol.Type == CoreLangLexer.IDENT).Select(c => c.GetText()).ToList();
            
            if (idents.Count == 2)
            {
                // object.method(...)
                string objectName = idents[0];
                string methodName = idents[1];
                
                var objSymbol = _currentScope.Resolve(objectName);
                if (objSymbol == null)
                    throw new SemanticException($"Variable '{objectName}' is not declared.",
                        context.Start.Line, context.Start.Column);
                        
                var classSymbol = _currentScope.Resolve(objSymbol.Type.Name) as ClassSymbol;
                if (classSymbol == null)
                    throw new SemanticException($"Type '{objSymbol.Type}' is not a class.",
                        context.Start.Line, context.Start.Column);
                        
                if (!classSymbol.Members.TryGetValue(methodName, out var member) || !(member is MethodSymbol method))
                    throw new SemanticException($"Class '{classSymbol.Name}' has no method '{methodName}'.",
                        context.Start.Line, context.Start.Column);
                        
                ValidateArguments(method, context.argListOpt(), methodName, context.Start.Line, context.Start.Column);
                return method.ReturnType;
            }
            else
            {
                // Global call
                var name = idents[0];
                var symbol = _currentScope.Resolve(name);

                if (symbol == null)
                    throw new SemanticException($"Function '{name}' is not declared.", 
                        context.Start.Line, context.Start.Column);

                if (symbol is MethodSymbol method)
                {
                    ValidateArguments(method, context.argListOpt(), name, context.Start.Line, context.Start.Column);
                    return method.ReturnType;
                }

                if (symbol is ClassSymbol)
                {
                    // Constructor-like call — returns the class type
                    return new TypeSymbol(name);
                }

                throw new SemanticException($"'{name}' is not a function.", 
                    context.Start.Line, context.Start.Column);
            }
        }
        
        private void ValidateArguments(MethodSymbol method, CoreLangParser.ArgListOptContext argListOpt, string name, int line, int col)
        {
            var args = argListOpt?.expr() ?? System.Array.Empty<CoreLangParser.ExprContext>();

            if (args.Length != method.Parameters.Count)
                throw new SemanticException(
                    $"Function '{name}' expects {method.Parameters.Count} arguments, got {args.Length}.",
                    line, col);

            for (int i = 0; i < args.Length; i++)
            {
                var argType = Visit(args[i]);
                var paramType = method.Parameters[i].Type;
                if (argType != null && !paramType.IsAssignableFrom(argType))
                    throw new SemanticException(
                        $"Argument {i + 1} of '{name}': expected '{paramType}', got '{argType}'.",
                        args[i].Start.Line, args[i].Start.Column);
            }
        }

        // ───────────────────────── SPECIAL FUNCTIONS ─────────────────────────

        public override TypeSymbol VisitLenCall([NotNull] CoreLangParser.LenCallContext context)
        {
            var name = context.IDENT().GetText();
            var symbol = _currentScope.Resolve(name);

            if (symbol == null)
                throw new SemanticException($"Variable '{name}' is not declared.",
                    context.Start.Line, context.Start.Column);

            if (!symbol.Type.IsArray())
                throw new SemanticException("'len()' only accepts arrays.",
                    context.Start.Line, context.Start.Column);

            return BuiltInTypes.Int;
        }

        public override TypeSymbol VisitAskCall([NotNull] CoreLangParser.AskCallContext context)
        {
            // ask() always returns s — the lvalue is where input is stored
            var lvalueCtx = context.lvalue();
            var varName = lvalueCtx.IDENT().GetText();
            var symbol = _currentScope.Resolve(varName);

            if (symbol == null)
                throw new SemanticException($"Variable '{varName}' is not declared.",
                    context.Start.Line, context.Start.Column);

            return BuiltInTypes.String;
        }

        public override TypeSymbol VisitConvertCall([NotNull] CoreLangParser.ConvertCallContext context)
        {
            var argType = Visit(context.expr());
            if (argType != null && !argType.IsString())
                throw new SemanticException("Conversion functions only accept type 's'.",
                    context.Start.Line, context.Start.Column);

            var convertName = context.convertName();
            if (convertName.CONV_INT() != null) return BuiltInTypes.Int;
            if (convertName.CONV_FLOAT() != null) return BuiltInTypes.Float;
            if (convertName.CONV_BOOL() != null) return BuiltInTypes.Bool;

            return null!;
        }

        public override TypeSymbol VisitShowCall([NotNull] CoreLangParser.ShowCallContext context)
        {
            // show() accepts any type
            Visit(context.expr());
            return null!;
        }

        // ───────────────────────── ACCESS ─────────────────────────

        public override TypeSymbol VisitMemberAccess([NotNull] CoreLangParser.MemberAccessContext context)
        {
            var idents = context.IDENT();
            var objName = idents[0].GetText();
            var memberName = idents[1].GetText();

            var symbol = _currentScope.Resolve(objName);
            if (symbol == null)
                throw new SemanticException($"Variable '{objName}' is not declared.",
                    context.Start.Line, context.Start.Column);

            // Resolve the class
            var classSymbol = _currentScope.Resolve(symbol.Type.Name) as ClassSymbol;
            if (classSymbol == null)
                throw new SemanticException($"Type '{symbol.Type}' is not a class.",
                    context.Start.Line, context.Start.Column);

            if (!classSymbol.Members.TryGetValue(memberName, out var member))
                throw new SemanticException(
                    $"Class '{classSymbol.Name}' has no member '{memberName}'.",
                    context.Start.Line, context.Start.Column);

            return member.Type;
        }

        public override TypeSymbol VisitIndexAccess([NotNull] CoreLangParser.IndexAccessContext context)
        {
            var name = context.IDENT().GetText();
            var symbol = _currentScope.Resolve(name);

            if (symbol == null)
                throw new SemanticException($"Variable '{name}' is not declared.",
                    context.Start.Line, context.Start.Column);

            if (!symbol.Type.IsArray())
                throw new SemanticException($"Variable '{name}' is not an array.",
                    context.Start.Line, context.Start.Column);

            var indexType = Visit(context.expr());
            if (indexType != null && indexType != BuiltInTypes.Int)
                throw new SemanticException("Array index must be of type 'i'.",
                    context.Start.Line, context.Start.Column);

            return ((ArrayTypeSymbol)symbol.Type).ElementType;
        }

        // ───────────────────────── TYPE RESOLUTION ─────────────────────────

        public override TypeSymbol VisitTypeRef([NotNull] CoreLangParser.TypeRefContext context)
        {
            return ResolveTypeRef(context);
        }

        private TypeSymbol ResolveTypeRef(CoreLangParser.TypeRefContext context)
        {
            var typeCore = context.typeCore();
            bool isNullable = context.nullOpt()?.QMARK() != null;

            TypeSymbol baseType;

            if (typeCore.arrayType() != null)
            {
                var arrayCtx = typeCore.arrayType();
                TypeSymbol elementType;

                if (arrayCtx.baseType() != null)
                    elementType = ResolveBaseType(arrayCtx.baseType());
                else
                    elementType = ResolveClassType(arrayCtx.classType(), context);

                baseType = new ArrayTypeSymbol(elementType, isNullable);
            }
            else if (typeCore.baseType() != null)
            {
                baseType = ResolveBaseType(typeCore.baseType());
                if (isNullable)
                    baseType = baseType.AsNullable();
            }
            else if (typeCore.classType() != null)
            {
                baseType = ResolveClassType(typeCore.classType(), context);
                if (isNullable)
                    baseType = baseType.AsNullable();
            }
            else
            {
                throw new SemanticException("Unknown type.",
                    context.Start.Line, context.Start.Column);
            }

            return baseType;
        }

        private TypeSymbol ResolveBaseType(CoreLangParser.BaseTypeContext context)
        {
            if (context.TYPE_I() != null) return BuiltInTypes.Int;
            if (context.TYPE_F() != null) return BuiltInTypes.Float;
            if (context.TYPE_B() != null) return BuiltInTypes.Bool;
            if (context.TYPE_S() != null) return BuiltInTypes.String;
            throw new SemanticException("Unknown base type.",
                context.Start.Line, context.Start.Column);
        }

        private TypeSymbol ResolveClassType(CoreLangParser.ClassTypeContext classCtx,
            CoreLangParser.TypeRefContext refCtx)
        {
            var className = classCtx.IDENT().GetText();
            var classSymbol = _currentScope.Resolve(className);
            if (classSymbol == null)
                throw new SemanticException($"Type '{className}' is not defined.",
                    refCtx.Start.Line, refCtx.Start.Column);
            return classSymbol.Type;
        }

        // ───────────────────────── HELPERS ─────────────────────────

        private void RegisterParams(MethodSymbol method, CoreLangParser.ParamListOptContext paramCtx)
        {
            if (paramCtx?.param() == null) return;

            foreach (var p in paramCtx.param())
            {
                var paramName = p.IDENT().GetText();
                var paramType = ResolveTypeRef(p.typeRef());
                var paramSymbol = new VariableSymbol(paramName, paramType, isParameter: true);
                method.Parameters.Add(paramSymbol);
                method.Scope.Define(paramSymbol, p.Start.Line, p.Start.Column);
            }
        }

        /// <summary>
        /// Validates that the last statement in a method's block is a 'gives' statement.
        /// </summary>
        private void ValidateGivesAtEnd(CoreLangParser.BlockContext block, TypeSymbol returnType)
        {
            var stmts = block.stmt();
            if (stmts == null || stmts.Length == 0)
            {
                throw new SemanticException(
                    "Method must end with a 'gives' statement.",
                    block.Start.Line, block.Start.Column);
            }

            var lastStmt = stmts[stmts.Length - 1];

            // Last statement must be a stmtSemi containing a givesStmt
            var stmtSemi = lastStmt.stmtSemi();
            if (stmtSemi == null)
            {
                throw new SemanticException(
                    "Method must end with a 'gives' statement.",
                    lastStmt.Start.Line, lastStmt.Start.Column);
            }

            var simpleStmt = stmtSemi.simpleStmt();
            if (simpleStmt?.givesStmt() == null)
            {
                throw new SemanticException(
                    "Method must end with a 'gives' statement.",
                    lastStmt.Start.Line, lastStmt.Start.Column);
            }
        }
    }
}
