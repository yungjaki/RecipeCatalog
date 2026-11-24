using RecipeCatalog.Core.Models;

namespace RecipeCatalog.Core.Data;

/// <summary>Достъп до данните за рецепти (абстракция, позволяваща подмяна на хранилището).</summary>
public interface IRecipeRepository
{
    IReadOnlyList<Category> GetCategories();
    int AddCategory(string name);
    SearchResult Search(SearchQuery query);
    Recipe? GetById(int id);
    int Add(RecipeInput input);
    bool Update(int id, RecipeInput input);
    bool Delete(int id);
    bool AddRating(int recipeId, int stars);
    int CountRecipes();
}
