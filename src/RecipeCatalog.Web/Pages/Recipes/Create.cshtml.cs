using Microsoft.AspNetCore.Mvc;
using RecipeCatalog.Core.Services;
using RecipeCatalog.Web.Services;

namespace RecipeCatalog.Web.Pages.Recipes;

public class CreateModel : RecipeFormModel
{
    public CreateModel(RecipeService service, ImageStorage storage) : base(service, storage)
    {
    }

    public void OnGet() => LoadCategories();

    public Task<IActionResult> OnPostAsync() =>
        SaveAsync(input => Service.Create(input), "Рецептата е добавена успешно.");
}
