namespace RecipeCatalog.Core.Models;

/// <summary>Продукт от рецептата: наименование и количество (напр. „Домати“, „3 бр.“).</summary>
public sealed record Ingredient(string Name, string Quantity);
