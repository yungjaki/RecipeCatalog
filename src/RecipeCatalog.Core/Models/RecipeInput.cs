namespace RecipeCatalog.Core.Models;

/// <summary>Данни, въведени от потребителя при добавяне или редакция на рецепта.</summary>
public sealed class RecipeInput
{
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string Instructions { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public int PrepTimeMinutes { get; set; }
    public List<Ingredient> Ingredients { get; set; } = new();
}
