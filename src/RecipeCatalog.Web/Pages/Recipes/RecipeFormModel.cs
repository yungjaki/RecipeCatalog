using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RecipeCatalog.Core.Models;
using RecipeCatalog.Core.Services;
using RecipeCatalog.Core.Validation;
using RecipeCatalog.Web.Services;

namespace RecipeCatalog.Web.Pages.Recipes;

/// <summary>Полетата на формата за рецепта (продуктите се въвеждат като текст – по един на ред).</summary>
public sealed class RecipeForm
{
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int PrepTimeMinutes { get; set; } = 30;
    public string IngredientsText { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public string ExistingImagePath { get; set; } = string.Empty;
}

/// <summary>Обща логика за страниците „Нова рецепта“ и „Редакция“ (DRY).</summary>
[RequestSizeLimit(3 * 1024 * 1024)]
public abstract class RecipeFormModel : PageModel
{
    public const string PlaceholderImage = "/images/placeholder.svg";

    private static readonly Dictionary<string, string> FieldMap = new()
    {
        [nameof(RecipeInput.Name)] = "Form.Name",
        [nameof(RecipeInput.CategoryId)] = "Form.CategoryId",
        [nameof(RecipeInput.PrepTimeMinutes)] = "Form.PrepTimeMinutes",
        [nameof(RecipeInput.Ingredients)] = "Form.IngredientsText",
        [nameof(RecipeInput.Instructions)] = "Form.Instructions"
    };

    private readonly ImageStorage _storage;

    protected RecipeFormModel(RecipeService service, ImageStorage storage)
    {
        Service = service;
        _storage = storage;
    }

    protected RecipeService Service { get; }

    [BindProperty] public RecipeForm Form { get; set; } = new();
    [BindProperty] public IFormFile? Image { get; set; }

    public IReadOnlyList<Category> Categories { get; private set; } = Array.Empty<Category>();

    protected void LoadCategories() => Categories = Service.GetCategories();

    /// <summary>Записва каченото изображение (ако има) и превръща формата в RecipeInput.</summary>
    protected async Task<RecipeInput?> BuildInputAsync()
    {
        var imagePath = string.IsNullOrEmpty(Form.ExistingImagePath) ? PlaceholderImage : Form.ExistingImagePath;
        if (Image is { Length: > 0 })
        {
            var (path, error) = await _storage.SaveAsync(Image);
            if (error is not null)
            {
                ModelState.AddModelError(nameof(Image), error);
                return null;
            }
            imagePath = path!;
        }

        return new RecipeInput
        {
            Name = Form.Name ?? string.Empty,
            CategoryId = Form.CategoryId,
            PrepTimeMinutes = Form.PrepTimeMinutes,
            Instructions = Form.Instructions ?? string.Empty,
            ImagePath = imagePath,
            Ingredients = IngredientLineParser.Parse(Form.IngredientsText)
        };
    }

    protected void AddErrors(IEnumerable<ValidationError> errors)
    {
        foreach (var error in errors)
        {
            var key = FieldMap.GetValueOrDefault(error.Field, string.Empty);
            ModelState.AddModelError(key, error.Message);
        }
    }

    /// <summary>Общ поток за запис: изграждане → валидация → запис → пренасочване към рецептата.</summary>
    protected async Task<IActionResult> SaveAsync(Func<RecipeInput, OperationResult<int>> save, string successMessage)
    {
        var input = await BuildInputAsync();
        if (input is not null)
        {
            var result = save(input);
            if (result.Success)
            {
                TempData["Message"] = successMessage;
                return RedirectToPage("/Recipes/Details", new { id = result.Value });
            }
            AddErrors(result.Errors);
        }

        LoadCategories();
        return Page();
    }
}
