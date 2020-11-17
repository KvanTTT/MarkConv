using System;
using Antlr4.Runtime;
using MarkConv.Html;

namespace MarkConv
{
    public class AntlrErrorListener : IAntlrErrorListener<IToken>, IAntlrErrorListener<int>
    {
        private readonly ILogger _logger;

        public AntlrErrorListener(ILogger logger) =>
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public void SyntaxError(IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg,
            RecognitionException e)
        {
            _logger.Warn($"Unexpected char at [{line},{charPositionInLine})");
        }

        public void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg,
            RecognitionException e)
        {
            _logger.Warn($"Parse error: {msg} at {((HtmlMarkdownToken)offendingSymbol).LineColumnSpan}");
        }
    }
}