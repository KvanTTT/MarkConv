lexer grammar HtmlLexer;

tokens { MARKDOWN_FRAGMENT }

TAG_OPEN:        '<' -> pushMode(TAG);
HTML_COMMENT:    '<!--' .*? '-->';
HTML_TEXT:       ~'<'+;

mode TAG;

TAG_CLOSE:       '>' -> popMode;
TAG_SLASH_CLOSE: '/>' -> popMode;
TAG_SLASH:       '/';
TAG_EQUALS:      '=' -> pushMode(ATTR);
TAG_NAME:        [:a-zA-Z][:a-zA-Z\-_.0-9]*;
TAG_WS:          [ \t\r\n]+ -> channel(HIDDEN);
TAG_ERROR:       . -> channel(HIDDEN), popMode;

mode ATTR;

ATTR_VALUE:      ( '"' ~[<"]* '"'
                 | '\'' ~[<']* '\''
                 | ~[ \t\r\n>]+
                 ) -> popMode;
ATTR_WS:         [ \t\r\n]+ -> channel(HIDDEN);
ATTR_ERROR:      . -> channel(HIDDEN), popMode;