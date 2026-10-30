using System;
using System.Text;
using System.Text.RegularExpressions;

namespace InternshipManagement.Application.Services
{
    /// <summary>Shared cleaning for text that came from the AI or from a client. Strips markup, caps length.</summary>
    internal static class TextCleaner
    {
        private static readonly Regex HtmlTags = new("<[^>]*>", RegexOptions.Compiled);
        private static readonly Regex Spaces = new("[ \\t]+", RegexOptions.Compiled);
        private static readonly Regex ExtraNewlines = new("\\n{3,}", RegexOptions.Compiled);
        private static readonly Regex NonKeyChars = new(@"[^\p{L}\p{N}#+]+", RegexOptions.Compiled);

        /// <summary>Strips HTML tags and control characters, collapses whitespace, caps the length. Null when empty.</summary>
        public static string? Clean(string? value, int maxLength, bool keepNewlines = false)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var text = HtmlTags.Replace(value, " ");

            var sb = new StringBuilder(text.Length);
            foreach (var ch in text)
            {
                if (ch == '\n' && keepNewlines) sb.Append('\n');
                else if (char.IsControl(ch)) sb.Append(' ');
                else sb.Append(ch);
            }

            text = Spaces.Replace(sb.ToString(), " ");
            if (keepNewlines)
            {
                text = Regex.Replace(text, " ?\\n ?", "\n");
                text = ExtraNewlines.Replace(text, "\n\n");
            }

            text = text.Trim();
            if (text.Length == 0) return null;

            return text.Length > maxLength ? text[..maxLength].TrimEnd() : text;
        }

        /// <summary>A short term (skill, certificate name): cleaned, no commas, at least 2 characters.</summary>
        public static string? Term(string? value, int maxLength)
        {
            var text = Clean(value, maxLength);
            if (text == null) return null;

            text = text.Replace(",", " ").Trim();
            return text.Length >= 2 ? text : null;
        }

        /// <summary>
        /// Comparison key: lower case, letters/digits/#/+ only. "C Sharp" and "C#" differ, but "Node.js" and
        /// "node js" match. Empty when nothing comparable is left.
        /// </summary>
        public static string Key(params string?[] parts)
        {
            var sb = new StringBuilder();
            foreach (var part in parts)
            {
                if (sb.Length > 0) sb.Append('|');
                sb.Append(NonKeyChars.Replace((part ?? string.Empty).ToLowerInvariant(), string.Empty));
            }
            return sb.ToString();
        }
    }
}
