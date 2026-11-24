using Microsoft.Data.Sqlite;
using RecipeCatalog.Core.Data;
using RecipeCatalog.Core.Models;

namespace RecipeCatalog.Tests;

/// <summary>
/// Изолирана SQLite база в паметта за всеки тест. Една връзка се държи отворена,
/// защото in-memory базата живее само докато има отворена връзка към нея.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _keepAlive;

    public TestDatabase()
    {
        var connectionString = $"Data Source=test-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        _keepAlive = new SqliteConnection(connectionString);
        _keepAlive.Open();

        Factory = new SqliteConnectionFactory(connectionString);
        DatabaseInitializer.EnsureCreated(Factory);
        Repository = new SqliteRecipeRepository(Factory);
    }

    public ISqliteConnectionFactory Factory { get; }
    public SqliteRecipeRepository Repository { get; }

    public int AddCategory(string name = "Салати") => Repository.AddCategory(name);

    public static RecipeInput Recipe(string name, int categoryId, params string[] ingredients) => new()
    {
        Name = name,
        CategoryId = categoryId,
        PrepTimeMinutes = 20,
        Instructions = "Нарежете продуктите и ги смесете в подходяща купа.",
        ImagePath = "/images/test.svg",
        Ingredients = ingredients.Select(i => new Ingredient(i, "1 бр.")).ToList()
    };

    public void Dispose() => _keepAlive.Dispose();
}
