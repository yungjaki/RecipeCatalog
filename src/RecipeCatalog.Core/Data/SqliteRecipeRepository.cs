using System.Globalization;
using Microsoft.Data.Sqlite;
using RecipeCatalog.Core.Models;
using RecipeCatalog.Core.Services;

namespace RecipeCatalog.Core.Data;

/// <summary>Реализация на хранилището върху SQLite с параметризирани заявки.</summary>
public sealed class SqliteRecipeRepository : IRecipeRepository
{
    private const string RatingsJoin =
        "LEFT JOIN (SELECT RecipeId, AVG(Stars) AS AvgRating, COUNT(*) AS RatingCount " +
        "FROM Ratings GROUP BY RecipeId) rt ON rt.RecipeId = r.Id";

    private readonly ISqliteConnectionFactory _factory;

    public SqliteRecipeRepository(ISqliteConnectionFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public IReadOnlyList<Category> GetCategories()
    {
        using var connection = _factory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name FROM Categories ORDER BY Id";
        using var reader = command.ExecuteReader();
        var result = new List<Category>();
        while (reader.Read())
        {
            result.Add(new Category(reader.GetInt32(0), reader.GetString(1)));
        }
        return result;
    }

    public int AddCategory(string name)
    {
        using var connection = _factory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Categories (Name) VALUES (@name); SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("@name", name.Trim());
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    public SearchResult Search(SearchQuery query)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var builder = SearchSqlBuilder.For(query);

        using var connection = _factory.Open();
        var total = CountMatches(connection, builder);
        var items = ReadPage(connection, builder, query.Sort, page, pageSize);
        return new SearchResult(items, total, page, pageSize);
    }

    public Recipe? GetById(int id)
    {
        using var connection = _factory.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT r.Id, r.Name, r.CategoryId, c.Name, r.Instructions, r.ImagePath, r.PrepTimeMinutes, " +
            "COALESCE(rt.AvgRating, 0), COALESCE(rt.RatingCount, 0) " +
            $"FROM Recipes r JOIN Categories c ON c.Id = r.CategoryId {RatingsJoin} WHERE r.Id = @id";
        command.Parameters.AddWithValue("@id", id);
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new Recipe
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            CategoryId = reader.GetInt32(2),
            CategoryName = reader.GetString(3),
            Instructions = reader.GetString(4),
            ImagePath = reader.GetString(5),
            PrepTimeMinutes = reader.GetInt32(6),
            AverageRating = reader.GetDouble(7),
            RatingCount = reader.GetInt32(8),
            Ingredients = ReadIngredients(connection, id)
        };
    }

    public int Add(RecipeInput input)
    {
        using var connection = _factory.Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            "INSERT INTO Recipes (Name, NameNormalized, CategoryId, Instructions, ImagePath, PrepTimeMinutes, CreatedAt) " +
            "VALUES (@name, @norm, @cat, @instr, @img, @prep, @created); SELECT last_insert_rowid();";
        AddRecipeParameters(command, input);
        command.Parameters.AddWithValue("@created", NowText());
        var id = Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);

        InsertIngredients(connection, transaction, id, input.Ingredients);
        transaction.Commit();
        return id;
    }

    public bool Update(int id, RecipeInput input)
    {
        using var connection = _factory.Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            "UPDATE Recipes SET Name = @name, NameNormalized = @norm, CategoryId = @cat, Instructions = @instr, " +
            "ImagePath = @img, PrepTimeMinutes = @prep WHERE Id = @id";
        AddRecipeParameters(command, input);
        command.Parameters.AddWithValue("@id", id);
        if (command.ExecuteNonQuery() == 0)
        {
            return false;
        }

        using var delete = connection.CreateCommand();
        delete.Transaction = transaction;
        delete.CommandText = "DELETE FROM Ingredients WHERE RecipeId = @id";
        delete.Parameters.AddWithValue("@id", id);
        delete.ExecuteNonQuery();

        InsertIngredients(connection, transaction, id, input.Ingredients);
        transaction.Commit();
        return true;
    }

    public bool Delete(int id)
    {
        using var connection = _factory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Recipes WHERE Id = @id";
        command.Parameters.AddWithValue("@id", id);
        return command.ExecuteNonQuery() > 0;
    }

    public bool AddRating(int recipeId, int stars)
    {
        using var connection = _factory.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO Ratings (RecipeId, Stars, CreatedAt) " +
            "SELECT @id, @stars, @created WHERE EXISTS (SELECT 1 FROM Recipes WHERE Id = @id)";
        command.Parameters.AddWithValue("@id", recipeId);
        command.Parameters.AddWithValue("@stars", stars);
        command.Parameters.AddWithValue("@created", NowText());
        return command.ExecuteNonQuery() > 0;
    }

    public int CountRecipes()
    {
        using var connection = _factory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Recipes";
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private static int CountMatches(SqliteConnection connection, SearchSqlBuilder builder)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM Recipes r {RatingsJoin} {builder.WhereClause}";
        AddParameters(command, builder);
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private static List<RecipeSummary> ReadPage(
        SqliteConnection connection, SearchSqlBuilder builder, SortOrder sort, int page, int pageSize)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT r.Id, r.Name, c.Name, r.ImagePath, r.PrepTimeMinutes, " +
            "COALESCE(rt.AvgRating, 0) AS AvgRating, COALESCE(rt.RatingCount, 0) AS RatingCount " +
            $"FROM Recipes r JOIN Categories c ON c.Id = r.CategoryId {RatingsJoin} " +
            $"{builder.WhereClause} {SearchSqlBuilder.OrderBy(sort)} LIMIT @limit OFFSET @offset";
        AddParameters(command, builder);
        command.Parameters.AddWithValue("@limit", pageSize);
        command.Parameters.AddWithValue("@offset", (page - 1) * pageSize);

        using var reader = command.ExecuteReader();
        var items = new List<RecipeSummary>();
        while (reader.Read())
        {
            items.Add(new RecipeSummary(reader.GetInt32(0), reader.GetString(1), reader.GetString(2),
                reader.GetString(3), reader.GetInt32(4), reader.GetDouble(5), reader.GetInt32(6)));
        }
        return items;
    }

    private static List<Ingredient> ReadIngredients(SqliteConnection connection, int recipeId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Name, Quantity FROM Ingredients WHERE RecipeId = @id ORDER BY SortOrder, Id";
        command.Parameters.AddWithValue("@id", recipeId);
        using var reader = command.ExecuteReader();
        var result = new List<Ingredient>();
        while (reader.Read())
        {
            result.Add(new Ingredient(reader.GetString(0), reader.GetString(1)));
        }
        return result;
    }

    private static void InsertIngredients(
        SqliteConnection connection, SqliteTransaction transaction, int recipeId, IEnumerable<Ingredient> ingredients)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            "INSERT INTO Ingredients (RecipeId, Name, NameNormalized, Quantity, SortOrder) " +
            "VALUES (@recipeId, @name, @norm, @qty, @order)";
        var pRecipe = command.Parameters.Add("@recipeId", SqliteType.Integer);
        var pName = command.Parameters.Add("@name", SqliteType.Text);
        var pNorm = command.Parameters.Add("@norm", SqliteType.Text);
        var pQty = command.Parameters.Add("@qty", SqliteType.Text);
        var pOrder = command.Parameters.Add("@order", SqliteType.Integer);

        var order = 0;
        foreach (var ingredient in ingredients)
        {
            pRecipe.Value = recipeId;
            pName.Value = ingredient.Name.Trim();
            pNorm.Value = TextNormalizer.Normalize(ingredient.Name);
            pQty.Value = ingredient.Quantity?.Trim() ?? string.Empty;
            pOrder.Value = order++;
            command.ExecuteNonQuery();
        }
    }

    private static void AddRecipeParameters(SqliteCommand command, RecipeInput input)
    {
        command.Parameters.AddWithValue("@name", input.Name.Trim());
        command.Parameters.AddWithValue("@norm", TextNormalizer.Normalize(input.Name));
        command.Parameters.AddWithValue("@cat", input.CategoryId);
        command.Parameters.AddWithValue("@instr", input.Instructions.Trim());
        command.Parameters.AddWithValue("@img", input.ImagePath ?? string.Empty);
        command.Parameters.AddWithValue("@prep", input.PrepTimeMinutes);
    }

    private static void AddParameters(SqliteCommand command, SearchSqlBuilder builder)
    {
        foreach (var (name, value) in builder.Parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }
    }

    private static string NowText() => DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
}
