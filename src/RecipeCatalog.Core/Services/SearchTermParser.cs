namespace RecipeCatalog.Core.Services;

/// <summary>Разделя текста за търсене на отделни термини („домати, сирене“ → [„домати“, „сирене“]).</summary>
public static class SearchTermParser
{
    public const int MaxTerms = 10;
    private static readonly char[] Separators = { ',', ';', '+' };

    public static IReadOnlyList<string> Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        return text
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries)
            .Select(TextNormalizer.Normalize)
            .Where(term => term.Length > 0)
            .Distinct()
            .Take(MaxTerms)
            .ToList();
    }
}
