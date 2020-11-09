parser grammar HtmlParser;

options { tokenVocab=HtmlLexer; }

root
    : content* EOF
    ;

content
    : element
    | HTML_COMMENT
    | HTML_TEXT
    | MARKDOWN_FRAGMENT
    ;

element
    : TAG_OPEN TAG_NAME attribute*
      (TAG_CLOSE (content* TAG_OPEN TAG_SLASH TAG_NAME TAG_CLOSE)? | TAG_SLASH_CLOSE)
    ;

attribute
    : TAG_NAME TAG_EQUALS ATTR_VALUE
    ;