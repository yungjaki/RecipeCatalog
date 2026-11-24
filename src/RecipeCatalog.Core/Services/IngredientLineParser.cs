using RecipeCatalog.Core.Models;

namespace RecipeCatalog.Core.Services;

/// <summary>
/// Превръща текст от формата (по един продукт на ред, „Име - количество“) в списък от продукти и обратно.
/// </summary>
public static class IngredientLineParser
{
    private const string Separator = " - ";

    public static List<Ingredient> Parse(string? text)
    {
        var result = new List<Ingredient>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return result;
        }

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines)
        {
            var ingredient = ParseLine(line);
            if (ingredient is not null)
            {
                result.Add(ingredient);
            }
        }

        return result;
    }

    public static string Format(IEnumerable<Ingredient> ingredients)
    {
        return string.Join("\n", ingredients.Select(i =>
            string.IsNullOrWhiteSpace(i.Quantity) ? i.Name : i.Name + Separator + i.Quantity));
    }

    private static Ingredient? ParseLine(string line)
    {
        var trimmed = line.Trim().TrimStart('-', '•', '*').Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        var index = trimmed.IndexOf(Separator, StringComparison.Ordinal);
        if (index < 0)
        {
            return new Ingredient(trimmed, string.Empty);
        }

        var name = trimmed[..index].Trim();
        var quantity = trimmed[(index + Separator.Length)..].Trim();
        return name.Length == 0 ? null : new Ingredient(name, quantity);
    }
}
