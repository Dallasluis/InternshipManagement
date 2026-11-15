namespace InternshipManagement.Application.Services
{
    /// <summary>
    /// Different spellings of the same skill ("MS Excel", "Microsoft Excel", "Excel") count as one when students
    /// are matched to internships. Add a line to <see cref="Groups"/> to teach it a new one.
    /// </summary>
    public static class SkillAliases
    {
        // the first name in each group is the one used for all the others
        private static readonly string[][] Groups =
        {
            new[] { "excel", "ms excel", "microsoft excel" },
            new[] { "word", "ms word", "microsoft word" },
            new[] { "powerpoint", "ms powerpoint", "microsoft powerpoint", "power point", "ppt" },
            new[] { "microsoft office", "ms office", "office 365", "microsoft 365", "m365" },
            new[] { "javascript", "js", "java script" },
            new[] { "typescript", "ts" },
            new[] { "c#", "c sharp", "csharp" },
            new[] { "sql", "structured query language" },
            new[] { "artificial intelligence", "ai" },
            new[] { "machine learning", "ml" },
            new[] { "autocad", "auto cad" },
            new[] { "communication", "communication skills", "verbal communication", "good communication" },
            new[] { "teamwork", "team work", "team player", "working in a team" },
            new[] { "problem solving", "problem solver" },
            new[] { "time management", "managing time" },
            new[] { "customer service", "customer care", "client service" },
            new[] { "project management", "managing projects" },
            new[] { "data analysis", "data analytics", "analysing data", "analyzing data" },
            new[] { "social media", "social media management", "social media marketing" },
            new[] { "bookkeeping", "book keeping" }
        };

        private static readonly Dictionary<string, string> Lookup = Build();

        public static string Normalize(string value)
        {
            var cleaned = string.Join(' ', value.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
            return Lookup.TryGetValue(cleaned, out var canonical) ? canonical : cleaned;
        }

        private static Dictionary<string, string> Build()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in Groups)
                foreach (var name in group)
                    map[name] = group[0];
            return map;
        }
    }
}
