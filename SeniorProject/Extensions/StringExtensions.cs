using System;
using System.Text.RegularExpressions;

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

            string cleaned = str.ToLowerInvariant();

            cleaned = Regex.Replace(cleaned, @"(\d+)\s*[xх]\s*(\d+)", "$1*$2");

            cleaned = cleaned
                .Replace(".", "")
                .Replace(",", "")
                .Replace("-", "")
                .Replace("/", "")
                .Replace("*", "")
                .Replace("=", "")
                .Replace("#", "")
                .Replace("\"", "");

            cleaned = Regex.Replace(cleaned, @"(\d+)\s*(кг|гр|г|л|мл)\b", m => {
                var unit = m.Groups[2].Value;
                return m.Groups[1].Value + (unit == "г" ? "гр" : unit);
            });

            var stopWords = new HashSet<string> { "за", "и", "от", "със", "в" };

            var words = cleaned.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                               .Where(w => !stopWords.Contains(w)) 
                               .ToArray();
            Array.Sort(words);

            return string.Join("", words);
        }
    }
}
