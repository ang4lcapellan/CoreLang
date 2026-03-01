parser grammar CoreLangParser;

// Usa los tokens definidos en CoreLangLexer
options { tokenVocab=CoreLangLexer; }

// Regla inicial
program : topLevelItem* EOF ;

// Elementos permitidos en el nivel superior
topLevelItem
    : useStmt
    | classDef
    | entryFuncDef
    | funcDef
    | stmtSemi
    | compoundStmt
    ;

// use Nombre;
useStmt : USE IDENT SEMI ;

// object Nombre { ... }
classDef : OBJECT IDENT classBlock ;
classBlock : LBRACE classMember* RBRACE ;

// Miembros dentro de object
classMember
    : varDecl SEMI
    | methodDef
    | stmtSemi
    | compoundStmt
    ;

// func Nombre(...) : tipo { ... }
methodDef : FUNC IDENT LPAREN paramListOpt RPAREN COLON typeRef block ;

entryFuncDef : ENTRY FUNC IDENT LPAREN paramListOpt RPAREN COLON typeRef block ;
funcDef      : FUNC IDENT LPAREN paramListOpt RPAREN COLON typeRef block ;

// Parámetros
paramListOpt
    : /* empty */
    | param (COMMA param)*
    ;

param : IDENT COLON typeRef ;

// Tipos
typeRef  : typeCore nullOpt ;
typeCore : arrayType | baseType | classType ;

baseType : TYPE_I | TYPE_F | TYPE_B | TYPE_S ;
classType: IDENT ;

arrayType: (baseType | classType) LBRACK INT_LIT RBRACK ;

nullOpt
    : /* empty */
    | QMARK
    ;

// Bloques y sentencias
block : LBRACE stmt* RBRACE ;

stmt : stmtSemi | compoundStmt ;

stmtSemi : simpleStmt SEMI ;

simpleStmt
    : varDecl
    | setStmt
    | givesStmt
    | expr
    ;

// declare x : tipo = expr
varDecl : DECLARE IDENT COLON typeRef initOpt ;

initOpt
    : /* empty */
    | ASSIGN expr
    ;

// set x = expr
setStmt : SET lvalue ASSIGN expr ;

lvalue
    : IDENT
    | IDENT LBRACK expr RBRACK
    ;

// gives expr
givesStmt : GIVES expr ;

// Control de flujo
compoundStmt : checkStmt | loopStmt | repeatStmt ;

// check (expr) { ... } otherwise { ... }
checkStmt : CHECK LPAREN expr RPAREN block otherwiseOpt ;

otherwiseOpt
    : /* empty */
    | OTHERWISE block
    ;

// loop ( init ; cond ; action ; ) { ... }
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

// repeat (expr) { ... }
repeatStmt : REPEAT LPAREN expr RPAREN block ;

// Expresiones (precedencia)
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

// Primarios
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

// Llamadas
callExpr : IDENT LPAREN argListOpt RPAREN ;

argListOpt
    : /* empty */
    | expr (COMMA expr)*
    ;

// Accesos
memberAccess : IDENT DOT IDENT ;
indexAccess  : IDENT LBRACK expr RBRACK ;

// Funciones predefinidas
lenCall   : LEN LPAREN IDENT RPAREN ;
askCall   : ASK LPAREN lvalue RPAREN ;

convertCall : convertName LPAREN expr RPAREN ;
convertName : CONV_INT | CONV_FLOAT | CONV_BOOL ;

showCall  : SHOW LPAREN expr RPAREN ;

// Arreglos literales
arrayLit : LBRACK elementsOpt RBRACK ;

elementsOpt
    : /* empty */
    | expr (COMMA expr)*
    ;

// Literales
literal
    : INT_LIT
    | FLOAT_LIT
    | STRING_LIT
    | TRUE
    | FALSE
    | NULL
    ;