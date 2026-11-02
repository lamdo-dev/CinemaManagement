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
        string createMoviesTable = @"
            CREATE TABLE IF NOT EXISTS Movies (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Description TEXT,
                DurationMinutes INTEGER NOT NULL,
                PosterUrl TEXT,
                Status INTEGER NOT NULL
            );";

        // Thực thi tạo bảng
        connection.Execute(createUsersTable);
        connection.Execute(createMoviesTable);
    }
}