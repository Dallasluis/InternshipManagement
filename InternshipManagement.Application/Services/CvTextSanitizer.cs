using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace InternshipManagement.Application.Services
{
    /// <summary>
    /// Removes contact details and personal identifiers from CV text BEFORE it leaves the API for n8n/Gemini.
    /// The AI only needs skills, education, experience and certificates, so it never sees phone numbers,
    /// emails, links, ID numbers, addresses or dates of birth. Date ranges such as "2021 - 2024" are kept.
    /// This is best effort (free text cannot be cleaned perfectly): it reduces what leaves the API, it does
    /// not guarantee that nothing personal is left.
    /// </summary>
    public static class CvTextSanitizer
    {
        private const int HeaderLinesToCheckForName = 6;

        private static readonly Regex Email = new(
            @"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}", RegexOptions.Compiled);

        private static readonly Regex Url = new(
            @"(?i)\b(?:https?://|www\.)\S+|\b(?:linkedin\.com|github\.com|gitlab\.com|behance\.net|facebook\.com|twitter\.com|instagram\.com)/\S*",
            RegexOptions.Compiled);

        // 7+ digits with optional spaces, dots, dashes, brackets, slashes. Never crosses a line break.
        private static readonly Regex DigitRun = new(
            @"(?<!\d)\+?\d[\d ().\-/\t]{5,}\d(?!\d)", RegexOptions.Compiled);

        // Passport / ID style codes: one or two letters then 6-9 digits, e.g. A1234567.
        private static readonly Regex LetterDigitId = new(
            @"\b[A-Za-z]{1,2}\d{6,9}\b", RegexOptions.Compiled);

        // "Date of birth: ...", "ID number: ...", "Address: ..." and similar labelled lines are dropped whole.
        private static readonly Regex SensitiveLine = new(
            @"^[ \t]*(?:residential address|postal address|physical address|home address|address|date of birth|d\.?o\.?b\.?|birth\s?date|born|id number|id no\.?|identity number|identity no\.?|national id|passport(?: number| no\.?)?|nationality|marital status|gender|sex|age|religion|home language)[ \t]*[:\-–][ \t]*.*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private static readonly Regex PoBoxLine = new(
            @"^[ \t]*p\.?\s?o\.?\s?box\b.*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private static readonly Regex Spaces = new(@"[ \t]{2,}", RegexOptions.Compiled);
        private static readonly Regex ExtraNewlines = new(@"\n{3,}", RegexOptions.Compiled);
        private static readonly Regex NonDigits = new(@"\D+", RegexOptions.Compiled);

        /// <param name="text">Raw text from the PDF.</param>
        /// <param name="namesToMask">The student's own names. Masked only in the first lines (the CV header),
        /// so a first name like "Ruby" does not erase the skill "Ruby" further down.</param>
        public static string Sanitize(string? text, IEnumerable<string?>? namesToMask = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var value = text.Replace("\r\n", "\n").Replace('\r', '\n');

            // Control characters (except new line and tab) and odd spaces
            var sb = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                if (ch == '\n' || ch == '\t') sb.Append(ch);
                else if (char.IsControl(ch)) sb.Append(' ');
                else if (char.IsWhiteSpace(ch)) sb.Append(' ');
                else sb.Append(ch);
            }
            value = sb.ToString();

            value = SensitiveLine.Replace(value, string.Empty);
            value = PoBoxLine.Replace(value, string.Empty);
            value = Email.Replace(value, " ");
            value = Url.Replace(value, " ");
            value = DigitRun.Replace(value, m => LooksLikeDatesOrShort(m.Value) ? m.Value : " ");
            value = LetterDigitId.Replace(value, " ");
            value = MaskNamesInHeader(value, namesToMask);

            // Tidy up: trim each line, collapse blanks
            var lines = value.Split('\n').Select(l => Spaces.Replace(l, " ").Trim());
            value = string.Join("\n", lines);
            value = ExtraNewlines.Replace(value, "\n\n");

            return value.Trim();
        }

        /// <summary>
        /// True when a digit run is NOT a phone or ID number: fewer than 7 digits, or only years
        /// (19xx / 20xx) and one or two digit months/days, such as "2021 - 2024" or "03/2021 - 08/2022".
        /// </summary>
        private static bool LooksLikeDatesOrShort(string match)
        {
            if (match.Count(char.IsDigit) < 7) return true;

            var tokens = NonDigits.Split(match).Where(t => t.Length > 0).ToList();
            if (tokens.Count == 0) return true;

            var years = 0;
            foreach (var token in tokens)
            {
                if (token.Length == 4 && (token.StartsWith("19", StringComparison.Ordinal) || token.StartsWith("20", StringComparison.Ordinal)))
                    years++;
                else if (token.Length > 2)
                    return false;
            }

            return years > 0;
        }

        private static string MaskNamesInHeader(string text, IEnumerable<string?>? names)
        {
            var parts = (names ?? Enumerable.Empty<string?>())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .SelectMany(n => n!.Split(new[] { ' ', '-', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                .Where(p => p.Length >= 2)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (parts.Count == 0) return text;

            var lines = text.Split('\n');
            var checkedLines = 0;
            for (var i = 0; i < lines.Length && checkedLines < HeaderLinesToCheckForName; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                checkedLines++;

                foreach (var part in parts)
                {
                    lines[i] = Regex.Replace(lines[i], @"(?i)\b" + Regex.Escape(part) + @"\b", " ");
                }
            }

            return string.Join("\n", lines);
        }
    }
}
