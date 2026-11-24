using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RecipeCatalog.Core.Models;
using RecipeCatalog.Core.Services;

namespace RecipeCatalog.Web.Pages.Recipes;

/// <summary>Подробности за рецепта, оценяване и изтриване.</summary>
public class DetailsModel : PageModel
{
    private readonly RecipeService _service;

    public DetailsModel(RecipeService service)
    {
        _service = service;
    }

    public Recipe Recipe { get; private set; } = null!;

    public IActionResult OnGet(int id)
    {
        var recipe = _service.GetById(id);
        if (recipe is null)
        {
            return NotFound();
        }

        Recipe = recipe;
        return Page();
    }

    public IActionResult OnPostRate(int id, int stars)
    {
        var result = _service.Rate(id, stars);
        TempData["Message"] = result.Success
            ? $"Благодарим! Оценихте рецептата с {stars} ★."
            : result.Errors[0].Message;
        return RedirectToPage(new { id });
    }

    public IActionResult OnPostDelete(int id)
    {
        if (!_service.Delete(id))
        {
            return NotFound();
        }

        TempData["Message"] = "Рецептата е изтрита.";
        return RedirectToPage("/Index");
    }
}
