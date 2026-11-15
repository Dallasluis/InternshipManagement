namespace InternshipManagement.Application.Services
{
    /// <summary>One word of a search, and the shortened form that is actually looked for.</summary>
    public sealed record SearchTerm(string Word, string Stem);

    /// <summary>
    /// Turns what a user typed into search words. Common endings are trimmed so "accounting" also finds
    /// "accountant" and "accounts", and "marketing" also finds "market research".
    /// </summary>
    public static class SearchTerms
    {
        private const int MaxTerms = 5;

        private static readonly string[] Suffixes =
        {
            "ations", "ation", "ants", "ant", "ance", "ings", "ing", "ers", "er", "ions", "ion", "ists", "ist", "ed", "es", "s"
        };

        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "and", "or", "the", "for", "with", "of", "in", "at", "to"
        };

        public static List<SearchTerm> Parse(string? keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return new List<SearchTerm>();

            return keyword
                .Split(new[] { ' ', ',', ';', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(w => w.ToLowerInvariant())
                .Where(w => w.Length >= 2 && !StopWords.Contains(w))
                .Distinct()
                .Take(MaxTerms)
                .Select(w => new SearchTerm(w, Stem(w)))
                .ToList();
        }

        private static string Stem(string word)
        {
            foreach (var suffix in Suffixes)
            {
                if (word.EndsWith(suffix, StringComparison.Ordinal) && word.Length - suffix.Length >= 4)
                    return word[..^suffix.Length];
            }

            return word;
        }
    }
}
