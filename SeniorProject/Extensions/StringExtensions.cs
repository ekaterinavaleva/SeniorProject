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

            string cleaned = str
                .Replace(".", "")
                .Replace(",", "")
                .Replace("-", "")
                .Replace("/", "")
                .Replace("*", "")
                .Replace("=", "")
                .Replace("#", "")
                .ToLowerInvariant();

            cleaned = Regex.Replace(cleaned, @"(\d+)\s*(кг|гр|л|мл)\b", "$1$2");

            var words = cleaned.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Array.Sort(words);

            return string.Join("", words);
        }
    }
}
