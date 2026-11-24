using Microsoft.Data.Sqlite;
using RecipeCatalog.Core.Data;
using RecipeCatalog.Core.Models;

namespace RecipeCatalog.Tests;

/// <summary>Интеграционни тестове: хранилище + истинска SQLite база (в паметта).</summary>
[TestFixture]
public class RepositoryTests
{
    private TestDatabase _db = null!;
    private int _salads;
    private int _soups;

    [SetUp]
    public void SetUp()
    {
        _db = new TestDatabase();
        _salads = _db.AddCategory("Салати");
        _soups = _db.AddCategory("Супи");
        _db.Repository.Add(TestDatabase.Recipe("Шопска салата", _salads, "Домати", "Краставици", "Сирене"));
        _db.Repository.Add(TestDatabase.Recipe("Таратор", _soups, "Кисело мляко", "Краставици", "Орехи"));
        _db.Repository.Add(TestDatabase.Recipe("Доматена супа", _soups, "Домати", "Лук"));
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private IEnumerable<string> Names(SearchQuery query) => _db.Repository.Search(query).Items.Select(r => r.Name);

    [Test]
    public void AddAndGetById_ReturnsRecipeWithIngredientsInOrder()
    {
        var id = _db.Repository.Add(TestDatabase.Recipe("Снежанка", _salads, "Кисело мляко", "Краставици", "Чесън"));
        var recipe = _db.Repository.GetById(id);

        Assert.That(recipe, Is.Not.Null);
        Assert.That(recipe!.Name, Is.EqualTo("Снежанка"));
        Assert.That(recipe.CategoryName, Is.EqualTo("Салати"));
        Assert.That(recipe.Ingredients.Select(i => i.Name), Is.EqualTo(new[] { "Кисело мляко", "Краставици", "Чесън" }));
    }

    [Test]
    public void GetById_MissingRecipe_ReturnsNull()
    {
        Assert.That(_db.Repository.GetById(12345), Is.Null);
    }

    [Test]
    public void Search_ByName_IsCaseInsensitiveForCyrillic()
    {
        var names = Names(new SearchQuery { Text = "ШОПСКА", Mode = SearchMode.Name });
        Assert.That(names, Is.EqualTo(new[] { "Шопска салата" }));
    }

    [Test]
    public void Search_ByIngredient_FindsAllRecipesContainingIt()
    {
        var names = Names(new SearchQuery { Text = "краставици", Mode = SearchMode.Ingredient });
        Assert.That(names, Is.EquivalentTo(new[] { "Шопска салата", "Таратор" }));
    }

    [Test]
    public void Search_ByIngredient_PartialWordMatches()
    {
        var names = Names(new SearchQuery { Text = "орех", Mode = SearchMode.Ingredient });
        Assert.That(names, Is.EqualTo(new[] { "Таратор" }));
    }

    [Test]
    public void Search_ByMultipleIngredients_RequiresAllOfThem()
    {
        var names = Names(new SearchQuery { Text = "домати, сирене", Mode = SearchMode.Ingredient });
        Assert.That(names, Is.EqualTo(new[] { "Шопска салата" }));
    }

    [Test]
    public void Search_AnyMode_MatchesNameOrIngredient()
    {
        var names = Names(new SearchQuery { Text = "домат" });
        Assert.That(names, Is.EquivalentTo(new[] { "Шопска салата", "Доматена супа" }));
    }

    [Test]
    public void Search_ByCategory_ReturnsOnlyThatCategory()
    {
        var names = Names(new SearchQuery { CategoryId = _soups });
        Assert.That(names, Is.EquivalentTo(new[] { "Таратор", "Доматена супа" }));
    }

    [Test]
    public void Search_WithMinRating_FiltersByAverage()
    {
        var tarator = _db.Repository.Search(new SearchQuery { Text = "Таратор" }).Items.Single().Id;
        _db.Repository.AddRating(tarator, 5);
        _db.Repository.AddRating(tarator, 4);

        var names = Names(new SearchQuery { MinRating = 4 });
        Assert.That(names, Is.EqualTo(new[] { "Таратор" }));
    }

    [Test]
    public void Search_SqlInjectionText_IsTreatedAsPlainText()
    {
        var result = _db.Repository.Search(new SearchQuery { Text = "'; DROP TABLE Recipes; --" });

        Assert.That(result.TotalCount, Is.Zero);
        Assert.That(_db.Repository.CountRecipes(), Is.EqualTo(3), "Таблицата трябва да е непокътната.");
    }

    [Test]
    public void Search_PercentSign_IsNotAWildcard()
    {
        Assert.That(_db.Repository.Search(new SearchQuery { Text = "%" }).TotalCount, Is.Zero);
    }

    [Test]
    public void Search_Paging_ReturnsRequestedPageAndTotal()
    {
        var result = _db.Repository.Search(new SearchQuery { PageSize = 2, Page = 2 });

        Assert.That(result.TotalCount, Is.EqualTo(3));
        Assert.That(result.TotalPages, Is.EqualTo(2));
        Assert.That(result.Items, Has.Count.EqualTo(1));
    }

    [Test]
    public void AddRating_CalculatesAverageAndCount()
    {
        var id = _db.Repository.Search(new SearchQuery { Text = "Шопска" }).Items.Single().Id;
        _db.Repository.AddRating(id, 5);
        _db.Repository.AddRating(id, 4);
        _db.Repository.AddRating(id, 3);

        var recipe = _db.Repository.GetById(id)!;
        Assert.That(recipe.AverageRating, Is.EqualTo(4.0).Within(0.001));
        Assert.That(recipe.RatingCount, Is.EqualTo(3));
    }

    [Test]
    public void AddRating_ForMissingRecipe_ReturnsFalse()
    {
        Assert.That(_db.Repository.AddRating(999, 5), Is.False);
    }

    [Test]
    public void AddRating_OutOfRange_IsRejectedByDatabaseConstraint()
    {
        var id = _db.Repository.Search(new SearchQuery()).Items[0].Id;
        Assert.Throws<SqliteException>(() => _db.Repository.AddRating(id, 9));
    }

    [Test]
    public void Update_ReplacesFieldsAndIngredients()
    {
        var id = _db.Repository.Search(new SearchQuery { Text = "Таратор" }).Items.Single().Id;
        var changed = _db.Repository.Update(id, TestDatabase.Recipe("Таратор с копър", _soups, "Копър"));

        var recipe = _db.Repository.GetById(id)!;
        Assert.That(changed, Is.True);
        Assert.That(recipe.Name, Is.EqualTo("Таратор с копър"));
        Assert.That(recipe.Ingredients.Select(i => i.Name), Is.EqualTo(new[] { "Копър" }));
    }

    [Test]
    public void Delete_RemovesRecipeAndCascadesToIngredients()
    {
        var id = _db.Repository.Search(new SearchQuery { Text = "Таратор" }).Items.Single().Id;
        _db.Repository.AddRating(id, 5);

        Assert.That(_db.Repository.Delete(id), Is.True);
        Assert.That(_db.Repository.GetById(id), Is.Null);
        Assert.That(CountRows("Ingredients", id) + CountRows("Ratings", id), Is.Zero);
    }

    [Test]
    public void SortByRating_PutsBestRatedFirst()
    {
        var all = _db.Repository.Search(new SearchQuery()).Items;
        _db.Repository.AddRating(all.Single(r => r.Name == "Доматена супа").Id, 5);
        _db.Repository.AddRating(all.Single(r => r.Name == "Таратор").Id, 3);

        var first = _db.Repository.Search(new SearchQuery { Sort = SortOrder.RatingDesc }).Items[0];
        Assert.That(first.Name, Is.EqualTo("Доматена супа"));
    }

    private long CountRows(string table, int recipeId)
    {
        using var connection = _db.Factory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {table} WHERE RecipeId = @id";
        command.Parameters.AddWithValue("@id", recipeId);
        return (long)command.ExecuteScalar()!;
    }
}
