using System.Diagnostics;
using RecipeCatalog.Core.Data;
using RecipeCatalog.Core.Models;
using RecipeCatalog.Core.Services;

namespace RecipeCatalog.Tests;

[TestFixture]
public class RecipeServiceTests
{
    private TestDatabase _db = null!;
    private RecipeService _service = null!;
    private int _category;

    [SetUp]
    public void SetUp()
    {
        _db = new TestDatabase();
        _service = new RecipeService(_db.Repository);
        _category = _db.AddCategory("Десерти");
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public void Create_ValidRecipe_IsSaved()
    {
        var result = _service.Create(TestDatabase.Recipe("Палачинки", _category, "Брашно", "Яйца"));

        Assert.That(result.Success, Is.True);
        Assert.That(_service.GetById(result.Value)!.Name, Is.EqualTo("Палачинки"));
    }

    [Test]
    public void Create_InvalidRecipe_IsNotSaved()
    {
        var result = _service.Create(TestDatabase.Recipe("x", _category));

        Assert.That(result.Success, Is.False);
        Assert.That(result.Errors, Is.Not.Empty);
        Assert.That(_db.Repository.CountRecipes(), Is.Zero);
    }

    [Test]
    public void Update_MissingRecipe_Fails()
    {
        var result = _service.Update(777, TestDatabase.Recipe("Палачинки", _category, "Брашно"));
        Assert.That(result.Success, Is.False);
    }

    [TestCase(0)]
    [TestCase(6)]
    public void Rate_InvalidStars_Fails(int stars)
    {
        var id = _service.Create(TestDatabase.Recipe("Палачинки", _category, "Брашно")).Value;
        Assert.That(_service.Rate(id, stars).Success, Is.False);
    }

    [Test]
    public void Search_ReturnsElapsedTime()
    {
        var result = _service.Search(new SearchQuery());
        Assert.That(result.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void Constructor_NullRepository_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _ = new RecipeService(null!));
    }
}

[TestFixture]
public class SearchSqlBuilderTests
{
    [Test]
    public void For_UserText_GoesIntoParametersNotIntoSql()
    {
        var builder = SearchSqlBuilder.For(new SearchQuery { Text = "' OR 1=1 --" });

        Assert.That(builder.WhereClause, Does.Not.Contain("OR 1=1"));
        Assert.That(builder.Parameters["@t0"], Is.EqualTo("%' or 1=1 --%"));
    }

    [Test]
    public void For_EmptyQuery_HasNoWhereClause()
    {
        Assert.That(SearchSqlBuilder.For(new SearchQuery()).WhereClause, Is.Empty);
    }

    [Test]
    public void For_TwoIngredients_CreatesTwoConditions()
    {
        var builder = SearchSqlBuilder.For(new SearchQuery { Text = "яйца, мляко", Mode = SearchMode.Ingredient });
        Assert.That(builder.Parameters.Keys, Is.EquivalentTo(new[] { "@t0", "@t1" }));
    }
}

[TestFixture]
public class SeedDataTests
{
    [Test]
    public void SeedIfEmpty_CreatesAtLeastThreeCategoriesWithCompleteRecipes()
    {
        using var db = new TestDatabase();
        SeedData.SeedIfEmpty(db.Repository);

        var categories = db.Repository.GetCategories();
        var recipes = db.Repository.Search(new SearchQuery { PageSize = 100 }).Items;

        Assert.That(categories, Has.Count.GreaterThanOrEqualTo(3));
        Assert.That(recipes.Select(r => r.CategoryName).Distinct().Count(), Is.EqualTo(categories.Count));
        Assert.That(recipes, Has.All.Matches<RecipeSummary>(r => r.ImagePath.Length > 0 && r.RatingCount > 0));
    }

    [Test]
    public void SeedIfEmpty_RunTwice_DoesNotDuplicate()
    {
        using var db = new TestDatabase();
        SeedData.SeedIfEmpty(db.Repository);
        var count = db.Repository.CountRecipes();
        SeedData.SeedIfEmpty(db.Repository);

        Assert.That(db.Repository.CountRecipes(), Is.EqualTo(count));
    }
}

/// <summary>Нефункционално изискване: търсене и филтриране под 1,5 секунди.</summary>
[TestFixture, Category("Performance")]
public class PerformanceTests
{
    private const int RecipeCount = 10_000;
    private const double LimitMs = 1500;
    private static readonly string[] Products = { "домати", "сирене", "лук", "яйца", "мляко", "брашно", "ориз", "чушки" };

    private TestDatabase _db = null!;

    [OneTimeSetUp]
    public void FillDatabase()
    {
        _db = new TestDatabase();
        var categories = new[] { _db.AddCategory("А"), _db.AddCategory("Б"), _db.AddCategory("В") };
        for (var i = 0; i < RecipeCount; i++)
        {
            var ingredients = Products.Skip(i % Products.Length).Take(4).ToArray();
            var id = _db.Repository.Add(TestDatabase.Recipe($"Рецепта {i}", categories[i % 3], ingredients));
            _db.Repository.AddRating(id, i % 5 + 1);
        }
    }

    [OneTimeTearDown]
    public void Cleanup() => _db.Dispose();

    [TestCase("рецепта 99", SearchMode.Name)]
    [TestCase("домати, сирене", SearchMode.Ingredient)]
    [TestCase("ориз", SearchMode.Any)]
    public void Search_On10000Recipes_IsUnderLimit(string text, SearchMode mode)
    {
        var query = new SearchQuery { Text = text, Mode = mode, MinRating = 3, Sort = SortOrder.RatingDesc };

        var stopwatch = Stopwatch.StartNew();
        var result = _db.Repository.Search(query);
        stopwatch.Stop();

        Assert.That(result.TotalCount, Is.GreaterThan(0));
        Assert.That(stopwatch.Elapsed.TotalMilliseconds, Is.LessThan(LimitMs));
    }
}
