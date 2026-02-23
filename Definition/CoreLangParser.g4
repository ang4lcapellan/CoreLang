parser grammar CoreLangParser;

options { tokenVocab=CoreLangLexer; }

program : topLevelItem* EOF ;

topLevelItem
    : useStmt
    | classDef
    | entryFuncDef
    | funcDef
    | stmtSemi
    | compoundStmt
    ;

useStmt : USE IDENT SEMI ;

classDef : OBJECT IDENT classBlock ;
classBlock : LBRACE classMember* RBRACE ;

classMember
    : varDecl SEMI
    | methodDef
    | stmtSemi
    | compoundStmt
    ;

methodDef : FUNC IDENT LPAREN paramListOpt RPAREN COLON typeRef block ;

entryFuncDef : ENTRY FUNC IDENT LPAREN paramListOpt RPAREN COLON typeRef block ;
funcDef      : FUNC        IDENT LPAREN paramListOpt RPAREN COLON typeRef block ;

paramListOpt
    : /* empty */
    | param (COMMA param)*
    ;

param : IDENT COLON typeRef ;

typeRef  : typeCore nullOpt ;
typeCore : arrayType | baseType | classType ;

baseType : TYPE_I | TYPE_F | TYPE_B | TYPE_S ;
classType: IDENT ;

arrayType: (baseType | classType) LBRACK INT_LIT RBRACK ;

nullOpt
    : /* empty */
    | QMARK
    ;

block : LBRACE stmt* RBRACE ;

stmt : stmtSemi | compoundStmt ;

stmtSemi : simpleStmt SEMI ;

simpleStmt
    : varDecl
    | setStmt
    | givesStmt
    | expr
    ;

varDecl : DECLARE IDENT COLON typeRef initOpt ;

initOpt
    : /* empty */
    | ASSIGN expr
    ;

setStmt : SET lvalue ASSIGN expr ;

lvalue
    : IDENT
    | IDENT LBRACK expr RBRACK
    ;

givesStmt : GIVES expr ;

compoundStmt : checkStmt | loopStmt | repeatStmt ;

checkStmt : CHECK LPAREN expr RPAREN block otherwiseOpt ;

otherwiseOpt
    : /* empty */
    | OTHERWISE block
    ;

loopStmt : LOOP LPAREN loopInit SEMI expr SEMI loopAction SEMI RPAREN block ;

loopInit
    : /* empty */
    | varDecl
    | setStmt
    ;

loopAction
    : /* empty */
    | setStmt
    | expr
    ;

repeatStmt : REPEAT LPAREN expr RPAREN block ;

// ----- expresiones (precedencia) -----

expr : orExpr ;

orExpr  : andExpr (OR andExpr)* ;
andExpr : eqExpr  (AND eqExpr)* ;

eqExpr : relExpr (eqOp relExpr)* ;
eqOp  : EQ | NEQ ;

relExpr: addExpr (relOp addExpr)* ;
relOp : GTE | LTE | GT | LT ;

addExpr: mulExpr (addOp mulExpr)* ;
addOp : PLUS | MINUS ;

mulExpr: unaryExpr (mulOp unaryExpr)* ;
mulOp : STAR | SLASH | PERCENT ;

unaryExpr
    : NOT unaryExpr
    | MINUS unaryExpr
    | primary
    ;

primary
    : literal
    | arrayLit
    | lenCall
    | askCall
    | convertCall
    | showCall
    | callExpr
    | memberAccess
    | indexAccess
    | IDENT
    | LPAREN expr RPAREN
    ;

callExpr : IDENT LPAREN argListOpt RPAREN ;

argListOpt
    : /* empty */
    | expr (COMMA expr)*
    ;

memberAccess : IDENT DOT IDENT ;
indexAccess  : IDENT LBRACK expr RBRACK ;

lenCall   : LEN LPAREN IDENT RPAREN ;
askCall   : ASK LPAREN lvalue RPAREN ;

convertCall : convertName LPAREN expr RPAREN ;
convertName : CONV_INT | CONV_FLOAT | CONV_BOOL ;

showCall  : SHOW LPAREN expr RPAREN ;

arrayLit : LBRACK elementsOpt RBRACK ;
elementsOpt
    : /* empty */
    | expr (COMMA expr)*
    ;

literal
    : INT_LIT
    | FLOAT_LIT
    | STRING_LIT
    | TRUE
    | FALSE
    | NULL
    ;