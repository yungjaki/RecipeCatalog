namespace RecipeCatalog.Core.Models;

/// <summary>Пълна информация за една рецепта.</summary>
public sealed class Recipe
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string Instructions { get; init; } = string.Empty;
    public string ImagePath { get; init; } = string.Empty;
    public int PrepTimeMinutes { get; init; }
    public double AverageRating { get; init; }
    public int RatingCount { get; init; }
    public IReadOnlyList<Ingredient> Ingredients { get; init; } = Array.Empty<Ingredient>();
}

/// <summary>Кратко представяне на рецепта за списъка с резултати.</summary>
public sealed record RecipeSummary(
    int Id,
    string Name,
    string CategoryName,
    string ImagePath,
    int PrepTimeMinutes,
    double AverageRating,
    int RatingCount);
