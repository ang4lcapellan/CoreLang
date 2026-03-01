lexer grammar CoreLangLexer;

// ================================
// 1) Palabras reservadas (keywords)
// ================================
// Estas palabras disparan reglas del parser como useStmt, classDef, funcDef, etc.
// Importante: al estar definidas antes que IDENT, ANTLR las reconoce como tokens
// reservados y NO como identificadores.

USE       : 'use';        // useStmt : USE IDENT SEMI ;
OBJECT    : 'object';     // classDef : OBJECT IDENT classBlock ;
ENTRY     : 'entry';      // entryFuncDef : ENTRY FUNC IDENT ...
FUNC      : 'func';       // funcDef/methodDef : FUNC IDENT ...
DECLARE   : 'declare';    // varDecl : DECLARE IDENT COLON typeRef initOpt ;
SET       : 'set';        // setStmt : SET lvalue ASSIGN expr ;
GIVES     : 'gives';      // givesStmt : GIVES expr ;
CHECK     : 'check';      // checkStmt : CHECK '(' expr ')' block otherwiseOpt ;
OTHERWISE : 'otherwise';  // otherwiseOpt : OTHERWISE block ;
LOOP      : 'loop';       // loopStmt : LOOP '(' loopInit ';' expr ';' loopAction ';' ')' block ;
REPEAT    : 'repeat';     // repeatStmt : REPEAT '(' expr ')' block ;

// ================================
// 2) Funciones/llamadas predefinidas
// ================================
// Se usan como "formas especiales" dentro de primary (lenCall, askCall, showCall, convertCall).

LEN       : 'len';        // lenCall  : LEN '(' IDENT ')' ;
ASK       : 'ask';        // askCall  : ASK '(' lvalue ')' ;
SHOW      : 'show';       // showCall : SHOW '(' expr ')' ;

// conversiones (convertCall)
CONV_INT  : 'convertToInt';      // convertName : CONV_INT | CONV_FLOAT | CONV_BOOL ;
CONV_FLOAT: 'convertToFloat';
CONV_BOOL : 'convertToBoolean';

// ================================
// 3) Operadores lógicos / booleanos
// ================================
// Se usan en expresiones por precedencia: orExpr, andExpr, unaryExpr.

AND       : 'and';        // andExpr : eqExpr (AND eqExpr)* ;
OR        : 'or';         // orExpr  : andExpr (OR andExpr)* ;
NOT       : 'not';        // unaryExpr : NOT unaryExpr | ...

// ================================
// 4) Literales reservados
// ================================
// Se consumen en la regla literal del parser.

TRUE      : 'true';       // literal : TRUE | ...
FALSE     : 'false';
NULL      : 'null';

// ================================
// 5) Tipos primitivos (type keywords)
// ================================
// Se usan dentro de baseType -> typeCore -> typeRef.

TYPE_I    : 'i';          // baseType : TYPE_I | TYPE_F | TYPE_B | TYPE_S ;
TYPE_F    : 'f';
TYPE_B    : 'b';
TYPE_S    : 's';

// ================================
// 6) Operadores relacionales / igualdad
// ================================
// eqExpr/relExpr usan estos tokens por precedencia.

EQ        : '==';         // eqOp  : EQ | NEQ ;
NEQ       : '!=';
GTE       : '>=';         // relOp : GTE | LTE | GT | LT ;
LTE       : '<=';
GT        : '>';
LT        : '<';

// ================================
// 7) Operadores aritméticos
// ================================
// addExpr/mulExpr usan estos tokens por precedencia.

PLUS      : '+';          // addOp : PLUS | MINUS ;
MINUS     : '-';
STAR      : '*';          // mulOp : STAR | SLASH | PERCENT ;
SLASH     : '/';
PERCENT   : '%';

// ================================
// 8) Asignación
// ================================
// Se usa en initOpt y setStmt.

ASSIGN    : '=';          // initOpt : ASSIGN expr | empty ;  setStmt : ... ASSIGN expr ;

// ================================
// 9) Delimitadores y separadores
// ================================
// Estructuran bloques, listas, llamadas, arrays, y terminan sentencias.

LPAREN    : '(';          // llamadas y control: ( expr )
RPAREN    : ')';
LBRACE    : '{';          // block, classBlock
RBRACE    : '}';
LBRACK    : '[';          // arrays: indexAccess, arrayType, arrayLit
RBRACK    : ']';

COMMA     : ',';          // listas de params/args/elements
SEMI      : ';';          // fin de sentencia y separadores en loop
COLON     : ':';          // anotación de tipo (IDENT : typeRef)
QMARK     : '?';          // nullOpt (nullable type)
DOT       : '.';          // memberAccess (IDENT . IDENT)

// ================================
// 10) Literales numéricos
// ================================
// Nota técnica: FLOAT_LIT debe aparecer antes que INT_LIT si quieres evitar casos
// donde "12.3" se parta raro. En tu lexer está bien porque FLOAT_LIT es más específico
// y ANTLR elige el match más largo; aún así, mantener FLOAT arriba suele ser buena práctica.

INT_LIT   : [0-9]+;                 // literal : INT_LIT
FLOAT_LIT : [0-9]+ '.' [0-9]+;       // literal : FLOAT_LIT

// ================================
// 11) Literales de string
// ================================
// Reconoce cadenas entre comillas dobles.
// Permite escapes básicos: \" \\ \n \t \r (por tu clase de escape).
// ~["\\\r\n] significa: cualquier char excepto comillas, backslash o salto de línea.

STRING_LIT: '"' ( '\\' ["\\ntr] | ~["\\\r\n] )* '"';  // literal : STRING_LIT

// ================================
// 12) Identificadores
// ================================
// Nombres de variables, funciones, clases.
// Importante: como keywords están antes, "use" se tokeniza como USE, no como IDENT.

IDENT     : [a-zA-Z] [a-zA-Z0-9_]*;

// ================================
// 13) Espacios y comentarios (se ignoran)
// ================================
// -> skip significa que el token no llega al parser.
// Esto simplifica reglas: tu parser no necesita <ws> explícito.

WS        : [ \t\r\n]+ -> skip;

// Comentario de línea: // hasta fin de línea
LINE_COMMENT : '//' ~[\r\n]* -> skip;

// Comentario de bloque: /* ... */ (no anidado)
BLOCK_COMMENT: '/*' .*? '*/' -> skip;