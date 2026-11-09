namespace CinemaManagement.Data;

using Dapper;
using Microsoft.Data.Sqlite;

public static class DatabaseInitializer
{
    private const string ConnectionString = "Data Source=cinema.db";

    public static void Initialize()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        // Lệnh SQL tạo bảng Users nếu chưa tồn tại
        string createUsersTable = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                PasswordHash TEXT NOT NULL,
                FullName TEXT NOT NULL,
                Role INTEGER NOT NULL
            );";

        // Lệnh SQL tạo bảng Movies
        // Bảng Movies
        string createMoviesTable = @"
            CREATE TABLE IF NOT EXISTS Movies (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Description TEXT,
                DurationMinutes INTEGER NOT NULL,
                Director TEXT,
                Genre TEXT,
                PosterUrl TEXT,
                ReleaseDate TEXT,
                Status INTEGER NOT NULL DEFAULT 0
            );
        ";

        connection.Execute(createUsersTable);
        connection.Execute(createMoviesTable);
        
        
        //bs
        // Bổ sung các cột nếu chưa tồn tại
        AddColumnIfNotExists(connection, "Movies", "Director", "TEXT");
        AddColumnIfNotExists(connection, "Movies", "Genre", "TEXT");
        AddColumnIfNotExists(connection, "Movies", "ReleaseDate", "TEXT");
        AddColumnIfNotExists(connection, "Movies", "PosterUrl", "TEXT");
    }

    private static void AddColumnIfNotExists(
        SqliteConnection connection,
        string table,
        string column,
        string type)
    {
        using var command = connection.CreateCommand();

        command.CommandText = $"PRAGMA table_info(\"{table}\");";

        bool columnExists = false;

        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                string existingColumn = reader["name"]?.ToString() ?? "";

                if (existingColumn.Equals(
                        column,
                        StringComparison.OrdinalIgnoreCase))
                {
                    columnExists = true;
                    break;
                }
            }
        }

        if (!columnExists)
        {
            using var alterCommand = connection.CreateCommand();

            alterCommand.CommandText =
                $"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {type};";

            alterCommand.ExecuteNonQuery();
        }
    }

}