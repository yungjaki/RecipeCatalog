using Microsoft.AspNetCore.Mvc;
using RecipeCatalog.Core.Services;
using RecipeCatalog.Web.Services;

namespace RecipeCatalog.Web.Pages.Recipes;

public class EditModel : RecipeFormModel
{
    public EditModel(RecipeService service, ImageStorage storage) : base(service, storage)
    {
    }

    public int Id { get; private set; }

    public IActionResult OnGet(int id)
    {
        var recipe = Service.GetById(id);
        if (recipe is null)
        {
            return NotFound();
        }

        Id = id;
        Form = new RecipeForm
        {
            Name = recipe.Name,
            CategoryId = recipe.CategoryId,
            PrepTimeMinutes = recipe.PrepTimeMinutes,
            Instructions = recipe.Instructions,
            ExistingImagePath = recipe.ImagePath,
            IngredientsText = IngredientLineParser.Format(recipe.Ingredients)
        };
        LoadCategories();
        return Page();
    }

    public Task<IActionResult> OnPostAsync(int id)
    {
        Id = id;
        return SaveAsync(input => Service.Update(id, input), "Промените са запазени.");
    }
}
