namespace RecipeCatalog.Core.Models;

/// <summary>Къде да се търси въведеният текст.</summary>
public enum SearchMode
{
    /// <summary>В името на рецептата или в продуктите.</summary>
    Any = 0,
    /// <summary>Само в името на рецептата.</summary>
    Name = 1,
    /// <summary>Само в продуктите; няколко продукта се разделят със запетая и всички трябва да присъстват.</summary>
    Ingredient = 2
}

/// <summary>Подредба на резултатите.</summary>
public enum SortOrder
{
    Name = 0,
    RatingDesc = 1,
    PrepTimeAsc = 2,
    Newest = 3
}

/// <summary>Критерии за търсене и филтриране на рецепти.</summary>
public sealed class SearchQuery
{
    public const int DefaultPageSize = 24;

    public string? Text { get; set; }
    public SearchMode Mode { get; set; } = SearchMode.Any;
    public int? CategoryId { get; set; }
    public int? MinRating { get; set; }
    public SortOrder Sort { get; set; } = SortOrder.Name;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = DefaultPageSize;
}

/// <summary>Една страница от резултатите плюс общия брой съвпадения.</summary>
public sealed record SearchResult(IReadOnlyList<RecipeSummary> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
