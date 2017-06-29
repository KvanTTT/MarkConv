using System.Text.RegularExpressions;

namespace HabraMark
{
    public static class MdRegex
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
        public static Regex DetailsOpenTagRegex = new Regex($@"<\s*details\s*>");
        public static Regex DetailsCloseTagRegex = new Regex($@"<\s*/details\s*>");
        public static Regex LinkRegex = new Regex(
            @"(!?)" +
            @"\[(([^\[\]]|\\\])+)\]" +
            @"\((#?)([^\)]+)\)", RegexOptions.Compiled);
    }
}
