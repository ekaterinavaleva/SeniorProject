using System;
using Xunit;
using SeniorProject.Extensions;

namespace SeniorProject.Tests
{
    public class StringExtensionsTests
    {
        [Fact]
        public void ToCleanSortedString_ShouldProduceSameResult_RegardlessOfWordOrder()
        {
            var str1 = "МЛЯКО ВЕРЕЯ 1Л";
            var str2 = "ВЕРЕЯ 1Л МЛЯКО";
            var str3 = "1Л ВЕРЕЯ МЛЯКО";

            var clean1 = str1.ToCleanSortedString();
            var clean2 = str2.ToCleanSortedString();
            var clean3 = str3.ToCleanSortedString();

            // assert that all three permutations result in the exact same normalized string
            Assert.Equal(clean1, clean2);
            Assert.Equal(clean1, clean3);
        }

        [Fact]
        public void ToCleanSortedString_ShouldRemoveSpecialCharacters()
        {
            var str1 = "=МЛЯКО-ВЕРЕЯ.1Л=";
            var str2 = "МЛЯКО ВЕРЕЯ 1Л";

            var clean1 = str1.ToCleanSortedString();
            var clean2 = str2.ToCleanSortedString();

            // assert that special characters are removed resulting in the same string
            Assert.Equal(clean1, clean2);
        }

        [Fact]
        public void GetStableHashCode_ShouldReturnSameHash_ForIdenticalStrings()
        {
            var str = "мляко";

            var hash1 = str.GetStableHashCode();
            var hash2 = str.GetStableHashCode();

            // assert that calling stable hash twice on the same string returns the same value
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void GetStableHashCode_ShouldReturnDifferentHash_ForDifferentStrings()
        {
            var str1 = "мляко";
            var str2 = "брашно";

            var hash1 = str1.GetStableHashCode();
            var hash2 = str2.GetStableHashCode();

            // assert that different strings return different hashes
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void ToCleanSortedString_ShouldReturnEmpty_ForNullOrWhitespace()
        {
            var str1 = "";
            var str2 = "   ";

            var clean1 = str1?.ToCleanSortedString();
            var clean2 = str2.ToCleanSortedString();

            // assert that null or whitespace values return an empty string
            Assert.Equal(string.Empty, clean1);
            Assert.Equal(string.Empty, clean2);
        }
    }
}
