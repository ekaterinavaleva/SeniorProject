using System;

namespace SeniorProject.Extensions
{
    public static class StringExtensions
    {
        public static int GetStableHashCode(this string str)
        {
            if (string.IsNullOrEmpty(str)) return 0;

            unchecked
            {
                int hash = (int)2166136261;
                foreach (char c in str)
                {
                    hash = (hash ^ c) * 16777619;
                }
                return hash;
            }
        }

        public static string ToCleanSortedString(this string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return string.Empty;

            string cleaned = str
                .Replace(".", " ")
                .Replace(",", " ")
                .Replace("-", " ")
                .Replace("/", " ")
                .Replace("*", " ")
                .Replace("=", " ")
                .ToLowerInvariant();

            var words = cleaned.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Array.Sort(words);

            return string.Join(" ", words);
        }
    }
}
