using RecipeCatalog.Core.Models;

namespace RecipeCatalog.Core.Validation;

/// <summary>Проверява данните за рецепта преди запис в базата.</summary>
public static class RecipeValidator
{
    public const int NameMin = 3;
    public const int NameMax = 100;
    public const int InstructionsMin = 20;
    public const int InstructionsMax = 5000;
    public const int IngredientNameMax = 60;
    public const int QuantityMax = 40;
    public const int MaxIngredients = 40;
    public const int PrepTimeMax = 1440;

    public static ValidationResult Validate(RecipeInput input, IReadOnlyCollection<int> validCategoryIds)
    {
        var result = new ValidationResult();
        ValidateName(input.Name, result);
        ValidateCategory(input.CategoryId, validCategoryIds, result);
        ValidateInstructions(input.Instructions, result);
        ValidatePrepTime(input.PrepTimeMinutes, result);
        ValidateIngredients(input.Ingredients, result);
        return result;
    }

    private static void ValidateName(string? name, ValidationResult result)
    {
        var length = name?.Trim().Length ?? 0;
        if (length < NameMin || length > NameMax)
        {
            result.Add(nameof(RecipeInput.Name), $"Името трябва да е между {NameMin} и {NameMax} символа.");
        }
    }

    private static void ValidateCategory(int categoryId, IReadOnlyCollection<int> validIds, ValidationResult result)
    {
        if (!validIds.Contains(categoryId))
        {
            result.Add(nameof(RecipeInput.CategoryId), "Изберете съществуваща категория.");
        }
    }

    private static void ValidateInstructions(string? text, ValidationResult result)
    {
        var length = text?.Trim().Length ?? 0;
        if (length < InstructionsMin || length > InstructionsMax)
        {
            result.Add(nameof(RecipeInput.Instructions),
                $"Начинът на приготвяне трябва да е между {InstructionsMin} и {InstructionsMax} символа.");
        }
    }

    private static void ValidatePrepTime(int minutes, ValidationResult result)
    {
        if (minutes < 1 || minutes > PrepTimeMax)
        {
            result.Add(nameof(RecipeInput.PrepTimeMinutes), $"Времето за приготвяне трябва да е от 1 до {PrepTimeMax} минути.");
        }
    }

    private static void ValidateIngredients(IReadOnlyCollection<Ingredient>? ingredients, ValidationResult result)
    {
        const string field = nameof(RecipeInput.Ingredients);
        if (ingredients is null || ingredients.Count == 0)
        {
            result.Add(field, "Добавете поне един продукт.");
            return;
        }

        if (ingredients.Count > MaxIngredients)
        {
            result.Add(field, $"Рецептата може да има най-много {MaxIngredients} продукта.");
        }

        if (ingredients.Any(i => string.IsNullOrWhiteSpace(i.Name) || i.Name.Trim().Length > IngredientNameMax))
        {
            result.Add(field, $"Всеки продукт трябва да има име до {IngredientNameMax} символа.");
        }

        if (ingredients.Any(i => (i.Quantity?.Length ?? 0) > QuantityMax))
        {
            result.Add(field, $"Количеството трябва да е до {QuantityMax} символа.");
        }
    }
}
