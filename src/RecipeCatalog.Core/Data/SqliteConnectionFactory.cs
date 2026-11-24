using Microsoft.Data.Sqlite;

namespace RecipeCatalog.Core.Data;

/// <summary>Отваря връзки към SQLite с включени външни ключове.</summary>
public interface ISqliteConnectionFactory
{
    SqliteConnection Open();
}

public sealed class SqliteConnectionFactory : ISqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Липсва низ за връзка към базата данни.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    public SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();
        return connection;
    }
}
