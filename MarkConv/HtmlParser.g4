parser grammar HtmlParser;

options { tokenVocab=HtmlLexer; }

@parser::members
{private MarkConv.ILogger _logger;

public HtmlParser(ITokenStream input, MarkConv.ILogger logger)
	: this(input)
{
	_logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
}

private void ProcessClosingTag()
{
	var openingSymbol = (MarkConv.Html.HtmlMarkdownToken)((ElementContext) RuleContext.Parent).TAG_NAME().Symbol;
	var tagCloseContext = (TagCloseContext) RuleContext;
	var closingSymbol = (MarkConv.Html.HtmlMarkdownToken)tagCloseContext.TAG_NAME().Symbol;
	var openingTagName = openingSymbol.Text;
	var closingTagName = closingSymbol.Text;
	if (!openingTagName.Equals(closingTagName, System.StringComparison.OrdinalIgnoreCase))
		_logger.Warn($"Incorrect nesting: element </{closingTagName}> at {closingSymbol.LineColumnSpan} closes <{openingTagName}> at {openingSymbol.LineColumnSpan}");
}
}

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
    : TAG_OPEN (
       TAG_NAME attribute* (TAG_CLOSE content* tagClose | TAG_SLASH_CLOSE) |
       voidElementTag attribute* (TAG_CLOSE | TAG_SLASH_CLOSE)
      )
    ;

tagClose
    : TAG_OPEN TAG_SLASH TAG_NAME {ProcessClosingTag();} TAG_CLOSE
    ;

voidElementTag
    : TAG_AREA
    | TAG_BASE
    | TAG_BR
    | TAG_COL
    | TAG_EMBED
    | TAG_HR
    | TAG_IMG
    | TAG_INPUT
    | TAG_LINK
    | TAG_META
    | TAG_PARAM
    | TAG_SOURCE
    | TAG_TRACK
    | TAG_WBR
    | TAG_CUT
    | TAG_LINKMAP
    ;

attribute
    : TAG_NAME TAG_EQUALS ATTR_VALUE
    ;