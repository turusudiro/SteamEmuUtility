using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace System
{
    class CaseInsensitiveCharComparer : EqualityComparer<char>
    {
        public override bool Equals(char x, char y)
        {
            return char.ToUpperInvariant(x) == char.ToUpperInvariant(y);
        }

        public override int GetHashCode(char obj)
        {
            return char.ToUpperInvariant(obj).GetHashCode();
        }
    }

    public static class StringExtensions
    {
        public static bool IsNullOrEmpty(this string source)
        {
            return string.IsNullOrEmpty(source);
        }

        public static bool IsNullOrWhiteSpace(this string source)
        {
            return string.IsNullOrWhiteSpace(source);
        }
        public static bool IsUri(this string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return false;
            }

            return Uri.IsWellFormedUriString(str, UriKind.Absolute);
        }
        public static string HtmlToPlainText(this string html)
        {
            const string tagWhiteSpace = @"(>|$)(\W|\n|\r)+<"; // matches one or more (white space or line breaks) between '>' and '<'
            const string stripFormatting = @"<[^>]*(>|$)"; // match any character between '<' and '>', even when end tag is missing
            const string lineBreak = @"<(br|BR)\s{0,1}\/{0,1}>"; // matches: <br>,<br/>,<br />,<BR>,<BR/>,<BR />
            var lineBreakRegex = new Regex(lineBreak, RegexOptions.Multiline);
            var stripFormattingRegex = new Regex(stripFormatting, RegexOptions.Multiline);
            var tagWhiteSpaceRegex = new Regex(tagWhiteSpace, RegexOptions.Multiline);

            var text = html;
            // Decode HTML specific characters
            text = System.Net.WebUtility.HtmlDecode(text);
            // Remove tag whitespace/line breaks
            text = tagWhiteSpaceRegex.Replace(text, "><");
            // Replace <br /> with line breaks
            text = lineBreakRegex.Replace(text, Environment.NewLine);
            // Strip formatting
            text = stripFormattingRegex.Replace(text, string.Empty);

            return text;
        }
    }
}
