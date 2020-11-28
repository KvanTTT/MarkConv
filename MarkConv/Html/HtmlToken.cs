using System;

namespace MarkConv.Html
{
    public class HtmlToken : HtmlMarkdownToken
    {
        public override string Text { get; }

        public override int Type { get; }

        public override int Channel { get; }

        public HtmlToken(TextFile file, int type, int index, int start, int stop, string text, int channel)
            : base(file, index, start, stop)
        {
            Type = type;
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Channel = channel;
        }
    }
}