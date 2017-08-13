using System.Text.RegularExpressions;

namespace HabraMark
{
    public static class MarkdownRegex
    {
        private static string space = @"[ \t]";
        public static string[] LineBreaks = new string[] { "\n", "\r\n" };
        public static char[] SpaceChars = new char[] { ' ', '\t' };

        public static Regex SpecialCharsRegex = new Regex($@"^(>|\*|-|\+|\d+\.|\||=)$", RegexOptions.Compiled);
        public static Regex SpecialItemRegex = new Regex($@"^{space}*(>|\|)", RegexOptions.Compiled);
        public static Regex ListItemRegex = new Regex($@"^{space}*(\*|-|\+|\d+\.){space}(.+)", RegexOptions.Compiled);
        public static Regex CodeSectionRegex = new Regex($@"^{space}*(~~~|```)", RegexOptions.Compiled | RegexOptions.Multiline);
        public static Regex HeaderRegex = new Regex($@"^{space}*(#+){space}*(.+)", RegexOptions.Compiled);
        public static Regex HeaderLineRegex = new Regex($@"^{space}*(-+|=+){space}*$", RegexOptions.Compiled);

        public static Regex DetailsTagRegex = new Regex(@"<\s*(/)?details\s*>", RegexOptions.Compiled);
        public static Regex SummaryTagsRegex = new Regex(@"<\s*summary\s*>(.*?)<\s*/summary\s*>", RegexOptions.Compiled);
        public static Regex SpoilerOpenTagRegex = new Regex(@"<\s*spoiler\s*title\s*=\s*""(.*?)""\s*>", RegexOptions.Compiled);
        public static Regex SpoilerCloseTagRegex = new Regex(@"<\s*/spoiler\s*>", RegexOptions.Compiled);
        public static Regex AnchorTagRegex = new Regex(@"<\s*anchor\s*>(.*?)<\s*/anchor\s*>", RegexOptions.Compiled);
        public static Regex LinkRegex = new Regex(
            @"(!?)" +
            @"\[(([^\[\]]|\\\])+)\]" +
            @"\((#?)([^\)]+)\)", RegexOptions.Compiled);
    }
}
