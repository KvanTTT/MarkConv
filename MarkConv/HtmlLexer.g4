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

// Void elements

TAG_AREA:        'area';
TAG_BASE:        'base';
TAG_BR:          'br';
TAG_COL:         'col';
TAG_EMBED:       'embed';
TAG_HR:          'hr';
TAG_IMG:         'img';
TAG_INPUT:       'input';
TAG_LINK:        'link';
TAG_META:        'meta';
TAG_PARAM:       'param';
TAG_SOURCE:      'source';
TAG_TRACK:       'track';
TAG_WBR:         'wbr';

TAG_CUT:         'cut'; // Habr cut element

TAG_NAME:        [:a-zA-Z][:a-zA-Z\-_.0-9]*;
TAG_WS:          [ \t\r\n]+ -> channel(HIDDEN);
TAG_ERROR:       . -> channel(HIDDEN), popMode;

mode ATTR;

ATTR_VALUE:      ( '"' ~[<"]* '"'
                 | '\'' ~[<']* '\''
                 | (~[ \t\r\n>/] | '/' ~'>')+
                 ) -> popMode;
ATTR_WS:         [ \t\r\n]+ -> channel(HIDDEN);
ATTR_ERROR:      . -> channel(HIDDEN), popMode;
