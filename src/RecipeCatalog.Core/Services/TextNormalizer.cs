using System.Text;

namespace RecipeCatalog.Core.Services;

/// <summary>
/// Нормализира текст за търсене. SQLite сравнява без значение на малки/главни букви само латиница,
/// затова в базата се пази отделна колона с нормализиран текст (малки букви, единични интервали).
/// </summary>
public static class TextNormalizer
{
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length);
        var previousWasSpace = false;
        foreach (var ch in text.Trim())
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!previousWasSpace)
                {
                    builder.Append(' ');
                }
                previousWasSpace = true;
                continue;
            }

            builder.Append(char.ToLowerInvariant(ch));
            previousWasSpace = false;
        }

        return builder.ToString();
    }

    /// <summary>Екранира символите %, _ и \, за да се търсят буквално в LIKE израз с ESCAPE '\'.</summary>
    public static string EscapeLike(string text)
    {
        return text
            .Replace(@"\", @"\\")
            .Replace("%", @"\%")
            .Replace("_", @"\_");
    }
}
