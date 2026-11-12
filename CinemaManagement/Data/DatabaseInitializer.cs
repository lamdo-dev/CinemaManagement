namespace CinemaManagement.Data;

using System;
using Dapper;
using Microsoft.Data.Sqlite;

public static class DatabaseInitializer
{
    private const string ConnectionString = "Data Source=cinema.db";

    public static void Initialize()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        string createUsersTable = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                PasswordHash TEXT NOT NULL,
                FullName TEXT NOT NULL,
                Role INTEGER NOT NULL,
                Email TEXT DEFAULT '',
                Phone TEXT DEFAULT '',
                RewardPoints INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT
            );";

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
            );";

        connection.Execute(createUsersTable);
        connection.Execute(createMoviesTable);

        // Bổ sung các cột còn thiếu cho bảng Users nếu DB đã tạo từ trước
        AddColumnIfNotExists(connection, "Users", "Email", "TEXT DEFAULT ''");
        AddColumnIfNotExists(connection, "Users", "Phone", "TEXT DEFAULT ''");
        AddColumnIfNotExists(connection, "Users", "RewardPoints", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfNotExists(connection, "Users", "CreatedAt", "TEXT");

        // Cập nhật giá trị mặc định cho các dòng cũ tránh lỗi null khi Dapper đọc dữ liệu
        connection.Execute(@"
            UPDATE Users 
            SET CreatedAt = datetime('now', 'localtime') 
            WHERE CreatedAt IS NULL OR CreatedAt = '';");

        // Bổ sung các cột cho bảng Movies
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
                if (existingColumn.Equals(column, StringComparison.OrdinalIgnoreCase))
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