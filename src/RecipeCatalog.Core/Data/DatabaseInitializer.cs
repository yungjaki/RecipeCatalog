using Microsoft.Data.Sqlite;

namespace RecipeCatalog.Core.Data;

/// <summary>Създава таблиците и индексите, ако още не съществуват.</summary>
public static class DatabaseInitializer
{
    private const string Schema = """
        CREATE TABLE IF NOT EXISTS Categories (
            Id    INTEGER PRIMARY KEY AUTOINCREMENT,
            Name  TEXT    NOT NULL UNIQUE
        );

        CREATE TABLE IF NOT EXISTS Recipes (
            Id               INTEGER PRIMARY KEY AUTOINCREMENT,
            Name             TEXT    NOT NULL,
            NameNormalized   TEXT    NOT NULL,
            CategoryId       INTEGER NOT NULL REFERENCES Categories(Id),
            Instructions     TEXT    NOT NULL,
            ImagePath        TEXT    NOT NULL DEFAULT '',
            PrepTimeMinutes  INTEGER NOT NULL CHECK (PrepTimeMinutes BETWEEN 1 AND 1440),
            CreatedAt        TEXT    NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Ingredients (
            Id              INTEGER PRIMARY KEY AUTOINCREMENT,
            RecipeId        INTEGER NOT NULL REFERENCES Recipes(Id) ON DELETE CASCADE,
            Name            TEXT    NOT NULL,
            NameNormalized  TEXT    NOT NULL,
            Quantity        TEXT    NOT NULL DEFAULT '',
            SortOrder       INTEGER NOT NULL DEFAULT 0
        );

        CREATE TABLE IF NOT EXISTS Ratings (
            Id         INTEGER PRIMARY KEY AUTOINCREMENT,
            RecipeId   INTEGER NOT NULL REFERENCES Recipes(Id) ON DELETE CASCADE,
            Stars      INTEGER NOT NULL CHECK (Stars BETWEEN 1 AND 5),
            CreatedAt  TEXT    NOT NULL
        );

        CREATE INDEX IF NOT EXISTS IX_Recipes_NameNormalized   ON Recipes(NameNormalized);
        CREATE INDEX IF NOT EXISTS IX_Recipes_CategoryId       ON Recipes(CategoryId);
        CREATE INDEX IF NOT EXISTS IX_Ingredients_RecipeId     ON Ingredients(RecipeId);
        CREATE INDEX IF NOT EXISTS IX_Ingredients_Normalized   ON Ingredients(NameNormalized, RecipeId);
        CREATE INDEX IF NOT EXISTS IX_Ratings_RecipeId         ON Ratings(RecipeId);
        """;

    public static void EnsureCreated(ISqliteConnectionFactory factory)
    {
        using var connection = factory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = Schema;
        command.ExecuteNonQuery();
    }
}
