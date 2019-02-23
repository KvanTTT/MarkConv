using System.Text.RegularExpressions;

namespace MarkConv
{
    public static class MarkdownRegex
    {
        private static string space = @"[ \t]";

        public static readonly string[] LineBreaks = { "\n", "\r\n" };
        public static readonly char[] SpaceChars = { ' ', '\t' };

        public static readonly Regex SpecialCharsRegex = new Regex($@"^(>|\*|-|\+|\d+\.|\||=)$", RegexOptions.Compiled);
        public static readonly Regex SpecialItemRegex = new Regex($@"^{space}*(>|\|)", RegexOptions.Compiled);
        public static readonly Regex ListItemRegex = new Regex($@"^{space}*(\*|-|\+|\d+\.){space}(.+)", RegexOptions.Compiled);
        public static readonly Regex CodeSectionRegex = new Regex($@"{space}*(~~~|```)", RegexOptions.Compiled | RegexOptions.Multiline);
        public static readonly Regex HeaderRegex = new Regex($@"^{space}*(#+){space}*(.+)", RegexOptions.Compiled);
        public static readonly Regex HeaderLineRegex = new Regex($@"^{space}*(-+|=+){space}*$", RegexOptions.Compiled);
        public static readonly Regex CommentRegex = new Regex(@"<!--(.|\n|\r)*?-->", RegexOptions.Compiled);

        public static readonly Regex DetailsOpenTagRegex = new Regex(@"<\s*details\s*>", RegexOptions.Compiled);
        public static readonly Regex DetailsCloseTagRegex = new Regex(@"<\s*/details\s*>", RegexOptions.Compiled);
        public static readonly Regex SummaryTagsRegex = new Regex(@"<\s*summary\s*>(.*?)<\s*/summary\s*>", RegexOptions.Compiled);
        public static readonly Regex SpoilerOpenTagRegex = new Regex(@"<\s*spoiler\s*title\s*=\s*""(.*?)""\s*>", RegexOptions.Compiled);
        public static readonly Regex SpoilerCloseTagRegex = new Regex(@"<\s*/spoiler\s*>", RegexOptions.Compiled);
        public static readonly Regex AnchorTagRegex = new Regex(@"<\s*anchor\s*>(.*?)<\s*/anchor\s*>", RegexOptions.Compiled);
        public static readonly Regex UrlRegex = new Regex(@"^https?://", RegexOptions.Compiled);
        public static readonly Regex SrcUrlRegex = new Regex(@"src\s*=\s*([^\s]+)", RegexOptions.Compiled);
        public static readonly Regex LinkRegex = new Regex(
            @"(!?)" +
            @"\[(([^\[\]]|\\\])+)\]" +
            @"\(([^\)]+)\)", RegexOptions.Compiled);
    }
}
