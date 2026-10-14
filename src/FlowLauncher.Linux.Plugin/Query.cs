namespace FlowLauncher.Linux.Plugin;

public class Query
{
    public string RawQuery { get; init; } = string.Empty;
    public string Search { get; init; } = string.Empty;
    public string ActionKeyword { get; init; } = string.Empty;
    public string[] Terms { get; init; } = Array.Empty<string>();
    public bool IsReQuery { get; init; }

    public static Query Parse(string rawQuery)
    {
        if (string.IsNullOrWhiteSpace(rawQuery))
            return new Query { RawQuery = rawQuery };

        var parts = rawQuery.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return new Query
        {
            RawQuery = rawQuery,
            Search = rawQuery.Trim(),
            ActionKeyword = string.Empty,
            Terms = parts
        };
    }

    public static Query ParseWithKeyword(string rawQuery, string actionKeyword)
    {
        var search = rawQuery.TrimStart();
        if (!string.IsNullOrEmpty(actionKeyword) && search.StartsWith(actionKeyword, StringComparison.OrdinalIgnoreCase))
            search = search[actionKeyword.Length..].TrimStart();
        return new Query
        {
            RawQuery = rawQuery,
            Search = search,
            ActionKeyword = actionKeyword,
            Terms = search.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        };
    }
}
