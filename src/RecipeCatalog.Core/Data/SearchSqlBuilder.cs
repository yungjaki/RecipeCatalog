using RecipeCatalog.Core.Models;
using RecipeCatalog.Core.Services;

namespace RecipeCatalog.Core.Data;

/// <summary>
/// Сглобява WHERE и ORDER BY клаузите за търсене. Потребителският текст НИКОГА не се слепва в SQL —
/// всеки термин става параметър (@t0, @t1, ...), което предпазва от SQL инжекция.
/// </summary>
public sealed class SearchSqlBuilder
{
    private readonly List<string> _conditions = new();

    public Dictionary<string, object> Parameters { get; } = new();

    public string WhereClause => _conditions.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", _conditions);

    public static SearchSqlBuilder For(SearchQuery query)
    {
        var builder = new SearchSqlBuilder();
        builder.AddTextConditions(query.Mode, SearchTermParser.Parse(query.Text));
        builder.AddCategory(query.CategoryId);
        builder.AddMinRating(query.MinRating);
        return builder;
    }

    public static string OrderBy(SortOrder sort) => sort switch
    {
        SortOrder.RatingDesc => "ORDER BY AvgRating DESC, RatingCount DESC, r.NameNormalized",
        SortOrder.PrepTimeAsc => "ORDER BY r.PrepTimeMinutes, r.NameNormalized",
        SortOrder.Newest => "ORDER BY r.Id DESC",
        _ => "ORDER BY r.NameNormalized"
    };

    private void AddTextConditions(SearchMode mode, IReadOnlyList<string> terms)
    {
        for (var i = 0; i < terms.Count; i++)
        {
            var parameter = $"@t{i}";
            Parameters[parameter] = "%" + TextNormalizer.EscapeLike(terms[i]) + "%";
            _conditions.Add(ConditionFor(mode, parameter));
        }
    }

    private static string ConditionFor(SearchMode mode, string parameter)
    {
        var byName = $@"r.NameNormalized LIKE {parameter} ESCAPE '\'";
        var byIngredient = "EXISTS (SELECT 1 FROM Ingredients i WHERE i.RecipeId = r.Id " +
                           $@"AND i.NameNormalized LIKE {parameter} ESCAPE '\')";
        return mode switch
        {
            SearchMode.Name => byName,
            SearchMode.Ingredient => byIngredient,
            _ => $"({byName} OR {byIngredient})"
        };
    }

    private void AddCategory(int? categoryId)
    {
        if (categoryId is > 0)
        {
            Parameters["@categoryId"] = categoryId.Value;
            _conditions.Add("r.CategoryId = @categoryId");
        }
    }

    private void AddMinRating(int? minRating)
    {
        if (minRating is >= 1 and <= 5)
        {
            Parameters["@minRating"] = minRating.Value;
            _conditions.Add("COALESCE(rt.AvgRating, 0) >= @minRating");
        }
    }
}
