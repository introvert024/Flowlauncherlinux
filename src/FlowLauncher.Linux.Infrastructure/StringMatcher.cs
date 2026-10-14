using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowLauncher.Linux.Infrastructure;

public class StringMatcher
{
    public static FuzzySearchResult MatchString(string pattern, string str)
    {
        if (string.IsNullOrEmpty(pattern)) return new FuzzySearchResult { Success = true, Score = 0 };
        if (string.IsNullOrEmpty(str)) return new FuzzySearchResult { Success = false };

        var lowerPattern = pattern.ToLowerInvariant();
        var lowerStr = str.ToLowerInvariant();

        // Exact match
        if (lowerStr == lowerPattern) return new FuzzySearchResult { Success = true, Score = 150, MatchData = Enumerable.Range(0, str.Length).ToList() };

        // StartsWith match
        if (lowerStr.StartsWith(lowerPattern))
        {
            var indices = Enumerable.Range(0, lowerPattern.Length).ToList();
            return new FuzzySearchResult { Success = true, Score = 120 + lowerPattern.Length, MatchData = indices };
        }

        // Contains match
        int idx = lowerStr.IndexOf(lowerPattern, StringComparison.Ordinal);
        if (idx >= 0)
        {
            var indices = Enumerable.Range(idx, lowerPattern.Length).ToList();
            return new FuzzySearchResult { Success = true, Score = 100, MatchData = indices };
        }

        // Fuzzy match - all pattern chars must appear in order in str
        var matchIndices = new List<int>();
        int pIdx = 0;
        int score = 0;
        for (int i = 0; i < lowerStr.Length && pIdx < lowerPattern.Length; i++)
        {
            if (lowerStr[i] == lowerPattern[pIdx])
            {
                matchIndices.Add(i);
                score += (lowerStr.Length - i);
                pIdx++;
            }
        }

        if (pIdx == lowerPattern.Length)
            return new FuzzySearchResult { Success = true, Score = score, MatchData = matchIndices };

        return new FuzzySearchResult { Success = false };
    }
}

public class FuzzySearchResult
{
    public bool Success { get; init; }
    public int Score { get; init; }
    public List<int> MatchData { get; init; } = new();
}
