using System;

namespace MarkConv.Cli
{
    public class OptionAttribute : Attribute
    {
        public string ShortName { get; }

        public string? LongName { get; }

        public string? HelpText { get; set; }

        public char Separator { get; set; } = ',';

        public bool Required { get; set; }

        public OptionAttribute()
            : this((char)0, "")
        {
        }

        public OptionAttribute(char shortName)
            :this(shortName, null)
        {
        }

        public OptionAttribute(string longName)
            : this((char)0, longName)
        {
        }

        public OptionAttribute(char shortName, string? longName)
        {
            ShortName = shortName == '\0' ? "" : shortName.ToString();
            LongName = longName;
        }

        public override string ToString()
        {
            return $"{ShortName}; {LongName}; {HelpText}";
        }
    }
}
