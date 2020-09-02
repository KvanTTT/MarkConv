using System;
using System.Text.RegularExpressions;

namespace MarkConv
{
    public class MatchResult
    {
        public ElementType Type { get; }

        public Match Match { get; }

        public MatchResult(ElementType type, Match match)
        {
            Type = type;
            Match = match ?? throw new ArgumentNullException(nameof(match));
        }

        public override string ToString() => $"Type: {Type}; Match: {Match}";
    }
}