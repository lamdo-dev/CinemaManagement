﻿using System;
using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CinemaManagement.Data;

public static class DatabaseInitializer
{
    public static void Initialize()
    {
        using var connection = DatabaseConnection.GetConnection();
        connection.Open();

        Console.WriteLine("DATABASE PATH:");
        Console.WriteLine(Path.GetFullPath("cinema.db"));

        // Bật kiểm tra khóa ngoại cho kết nối hiện tại.
        connection.Execute("PRAGMA foreign_keys = ON;");

        CreateUsersTable(connection);
        CreateMoviesTable(connection);
        CreateRoomsTable(connection);
        CreateSeatsTable(connection);
        CreateShowtimesTable(connection);
        CreateProductsTable(connection);

        // Tạo bảng quản lý giá vé
        CreatePricingRulesTable(connection);
        // Tạo bảng quản lý khuyến mãi
        CreatePromotionsTable(connection);

        CreateIndexes(connection);

        Console.WriteLine("Database initialized successfully!");

        var tables = connection.Query<string>(@"
            SELECT name
            FROM sqlite_master
            WHERE type = 'table'
              AND name NOT LIKE 'sqlite_%'
            ORDER BY name;
        ");

        Console.WriteLine("DANH SÁCH BẢNG DATABASE:");

        foreach (var table in tables)
        {
            Console.WriteLine(table);
        }
    }

    // =========================================
    // PROMOTIONS
    // =========================================

    private static void CreatePromotionsTable(SqliteConnection connection)
    {
        connection.Execute(@"
        CREATE TABLE IF NOT EXISTS Promotions
        (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Code TEXT NOT NULL COLLATE NOCASE UNIQUE,
            Name TEXT NOT NULL,
            DiscountType TEXT NOT NULL,
            DiscountValue NUMERIC NOT NULL DEFAULT 0,
            MaxDiscount NUMERIC NOT NULL DEFAULT 0,
            MinimumOrder NUMERIC NOT NULL DEFAULT 0,
            StartDate TEXT NOT NULL,
            EndDate TEXT NOT NULL,
            IsEnabled INTEGER NOT NULL DEFAULT 1
        );
    ");
    }

    // =========================================
    // USERS
    // =========================================

    private static void CreateUsersTable(SqliteConnection connection)
    {
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                PasswordHash TEXT NOT NULL DEFAULT '',
                FullName TEXT NOT NULL,
                Role INTEGER NOT NULL DEFAULT 0,
                Email TEXT DEFAULT '',
                Phone TEXT DEFAULT '',
                RewardPoints INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT
            );
        ");

        AddColumnIfNotExists(connection, "Users", "PasswordHash",
            "TEXT NOT NULL DEFAULT ''");

        AddColumnIfNotExists(connection, "Users", "Email", "TEXT DEFAULT ''");
        AddColumnIfNotExists(connection, "Users", "Phone", "TEXT DEFAULT ''");

        AddColumnIfNotExists(connection, "Users", "RewardPoints",
            "INTEGER NOT NULL DEFAULT 0");

        AddColumnIfNotExists(connection, "Users", "CreatedAt", "TEXT");

        connection.Execute(@"
            UPDATE Users
            SET CreatedAt = datetime('now', 'localtime')
            WHERE CreatedAt IS NULL OR CreatedAt = '';
        ");
    }

    // =========================================
    // MOVIES
    // =========================================

    private static void CreateMoviesTable(SqliteConnection connection)
    {
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Movies (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Description TEXT DEFAULT '',
                DurationMinutes INTEGER NOT NULL DEFAULT 0,
                Director TEXT DEFAULT '',
                Genre TEXT DEFAULT '',
                PosterUrl TEXT DEFAULT '',
                ReleaseDate TEXT,
                Status INTEGER NOT NULL DEFAULT 0
            );
        ");

        AddColumnIfNotExists(connection, "Movies", "Description",
            "TEXT DEFAULT ''");

        AddColumnIfNotExists(connection, "Movies", "DurationMinutes",
            "INTEGER NOT NULL DEFAULT 0");

        AddColumnIfNotExists(connection, "Movies", "Director",
            "TEXT DEFAULT ''");

        AddColumnIfNotExists(connection, "Movies", "Genre",
            "TEXT DEFAULT ''");

        AddColumnIfNotExists(connection, "Movies", "PosterUrl",
            "TEXT DEFAULT ''");

        AddColumnIfNotExists(connection, "Movies", "ReleaseDate", "TEXT");

        AddColumnIfNotExists(connection, "Movies", "Status",
            "INTEGER NOT NULL DEFAULT 0");
    }

    // =========================================
    // ROOMS
    // =========================================

    private static void CreateRoomsTable(SqliteConnection connection)
    {
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Rooms (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                TotalSeats INTEGER NOT NULL DEFAULT 0,
                TotalRows INTEGER NOT NULL DEFAULT 0,
                TotalColumns INTEGER NOT NULL DEFAULT 0
            );
        ");
    }

    // =========================================
    // SEATS
    // =========================================

    private static void CreateSeatsTable(SqliteConnection connection)
    {
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Seats (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                RoomId INTEGER NOT NULL,
                SeatNumber TEXT NOT NULL,
                RowIndex INTEGER NOT NULL,
                ColumnIndex INTEGER NOT NULL,
                Type INTEGER NOT NULL DEFAULT 0,
                PriceMultiplier NUMERIC NOT NULL DEFAULT 1.0,

                FOREIGN KEY (RoomId)
                    REFERENCES Rooms(Id)
                    ON DELETE CASCADE,

                UNIQUE(RoomId, SeatNumber)
            );
        ");
    }

    // =========================================
    // SHOWTIMES
    // =========================================

    private static void CreateShowtimesTable(SqliteConnection connection)
    {
        connection.Execute(@"
        CREATE TABLE IF NOT EXISTS Showtimes (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            MovieId INTEGER NOT NULL,
            RoomId INTEGER NOT NULL,
            StartTime TEXT NOT NULL,
            EndTime TEXT NOT NULL,
            BasePrice NUMERIC NOT NULL DEFAULT 0,
            Format TEXT NOT NULL DEFAULT '2D',

            FOREIGN KEY (MovieId)
                REFERENCES Movies(Id),

            FOREIGN KEY (RoomId)
                REFERENCES Rooms(Id)
        );
    ");

        // Bổ sung cột Format cho database cũ
        AddColumnIfNotExists(
            connection,
            "Showtimes",
            "Format",
            "TEXT NOT NULL DEFAULT '2D'"
        );
    }

    // =========================================
    // PRODUCTS
    // =========================================

    private static void CreateProductsTable(SqliteConnection connection)
    {
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Products (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Category TEXT NOT NULL,
                Price NUMERIC NOT NULL DEFAULT 0,
                StockQuantity INTEGER NOT NULL DEFAULT 0,
                ImageUrl TEXT DEFAULT '',
                IsActive INTEGER NOT NULL DEFAULT 1
            );
        ");
    }

    // =========================================
    // PRICING RULES
    // =========================================

    private static void CreatePricingRulesTable(SqliteConnection connection)
    {
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS PricingRules (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Format TEXT NOT NULL,
                SeatType TEXT NOT NULL,
                DayType TEXT NOT NULL,
                TimeSlot TEXT NOT NULL,
                BasePrice NUMERIC NOT NULL,
                Surcharge NUMERIC NOT NULL DEFAULT 0,

                UNIQUE (Format, SeatType, DayType, TimeSlot)
            );
        ");
    }

    // =========================================
    // INDEXES
    // =========================================

    private static void CreateIndexes(SqliteConnection connection)
    {
        connection.Execute(@"
            CREATE INDEX IF NOT EXISTS IX_Seats_RoomId
            ON Seats(RoomId);

            CREATE INDEX IF NOT EXISTS IX_Showtimes_MovieId
            ON Showtimes(MovieId);

            CREATE INDEX IF NOT EXISTS IX_Showtimes_RoomId_StartTime
            ON Showtimes(RoomId, StartTime);

            CREATE INDEX IF NOT EXISTS IX_Products_Category
            ON Products(Category);

            CREATE INDEX IF NOT EXISTS IX_PricingRules_Format
            ON PricingRules(Format);

            CREATE INDEX IF NOT EXISTS IX_PricingRules_DayType
            ON PricingRules(DayType);

            CREATE INDEX IF NOT EXISTS IX_Promotions_StartDate_EndDate
            ON Promotions(StartDate, EndDate);

        ");
    }

    // =========================================
    // ADD COLUMN IF NOT EXISTS
    // =========================================

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
                string existingColumn =
                    reader["name"]?.ToString() ?? "";

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