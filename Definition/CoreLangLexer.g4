lexer grammar CoreLangLexer;

USE       : 'use';
OBJECT    : 'object';
ENTRY     : 'entry';
FUNC      : 'func';
DECLARE   : 'declare';
SET       : 'set';
GIVES     : 'gives';
CHECK     : 'check';
OTHERWISE : 'otherwise';
LOOP      : 'loop';
REPEAT    : 'repeat';

LEN       : 'len';
ASK       : 'ask';
SHOW      : 'show';

CONV_INT  : 'convertToInt';
CONV_FLOAT: 'convertToFloat';
CONV_BOOL : 'convertToBoolean';

AND       : 'and';
OR        : 'or';
NOT       : 'not';

TRUE      : 'true';
FALSE     : 'false';
NULL      : 'null';

TYPE_I    : 'i';
TYPE_F    : 'f';
TYPE_B    : 'b';
TYPE_S    : 's';

EQ        : '==';
NEQ       : '!=';
GTE       : '>=';
LTE       : '<=';
GT        : '>';
LT        : '<';

PLUS      : '+';
MINUS     : '-';
STAR      : '*';
SLASH     : '/';
PERCENT   : '%';

ASSIGN    : '=';

LPAREN    : '(';
RPAREN    : ')';
LBRACE    : '{';
RBRACE    : '}';
LBRACK    : '[';
RBRACK    : ']';

COMMA     : ',';
SEMI      : ';';
COLON     : ':';
QMARK     : '?';
DOT       : '.';

INT_LIT   : [0-9]+;
FLOAT_LIT : [0-9]+ '.' [0-9]+;

STRING_LIT: '"' ( '\\' ["\\ntr] | ~["\\\r\n] )* '"';

IDENT     : [a-zA-Z] [a-zA-Z0-9_]*;

WS        : [ \t\r\n]+ -> skip;

LINE_COMMENT : '//' ~[\r\n]* -> skip;
BLOCK_COMMENT: '/*' .*? '*/' -> skip;