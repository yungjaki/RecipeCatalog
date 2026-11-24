using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RecipeCatalog.Core.Models;
using RecipeCatalog.Core.Services;

namespace RecipeCatalog.Web.Pages;

/// <summary>Начална страница: търсене и филтриране на рецепти.</summary>
public class IndexModel : PageModel
{
    public const int MaxQueryLength = 200;
    private readonly RecipeService _service;

    public IndexModel(RecipeService service)
    {
        _service = service;
    }

    [BindProperty(SupportsGet = true, Name = "q")] public string? Query { get; set; }
    [BindProperty(SupportsGet = true, Name = "mode")] public SearchMode Mode { get; set; }
    [BindProperty(SupportsGet = true, Name = "cat")] public int? CategoryId { get; set; }
    [BindProperty(SupportsGet = true, Name = "rating")] public int? MinRating { get; set; }
    [BindProperty(SupportsGet = true, Name = "sort")] public SortOrder Sort { get; set; }
    [BindProperty(SupportsGet = true, Name = "p")] public int PageNumber { get; set; } = 1;

    public IReadOnlyList<Category> Categories { get; private set; } = Array.Empty<Category>();
    public TimedSearchResult? Result { get; private set; }
    public bool HasFilters => !string.IsNullOrWhiteSpace(Query) || CategoryId is > 0 || MinRating is > 0;

    public void OnGet()
    {
        if (Query?.Length > MaxQueryLength)
        {
            Query = Query[..MaxQueryLength];
        }

        Categories = _service.GetCategories();
        Result = _service.Search(new SearchQuery
        {
            Text = Query,
            Mode = Enum.IsDefined(Mode) ? Mode : SearchMode.Any,
            CategoryId = CategoryId,
            MinRating = MinRating,
            Sort = Enum.IsDefined(Sort) ? Sort : SortOrder.Name,
            Page = PageNumber
        });
    }

    /// <summary>Строи параметрите на линк, като запазва текущите филтри и сменя само подадените.</summary>
    public Dictionary<string, string?> RouteWith(int? category = -1, int? page = null)
    {
        return new Dictionary<string, string?>
        {
            ["q"] = Query,
            ["mode"] = Mode == SearchMode.Any ? null : Mode.ToString(),
            ["cat"] = (category == -1 ? CategoryId : category)?.ToString(),
            ["rating"] = MinRating?.ToString(),
            ["sort"] = Sort == SortOrder.Name ? null : Sort.ToString(),
            ["p"] = page is > 1 ? page.ToString() : null
        };
    }
}
