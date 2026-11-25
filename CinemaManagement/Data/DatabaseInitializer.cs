using System;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CinemaManagement.Data;

public static class DatabaseInitializer
{
    public static void Initialize()
    {
        using var connection = DatabaseConnection.GetConnection();
        connection.Open();

        // Bật khóa ngoại
        connection.Execute("PRAGMA foreign_keys = ON;");

        // Các bảng hiện có
        CreateUsersTable(connection);
        SeedAdminUser(connection);
        CreateMoviesTable(connection);
        CreateRoomsTable(connection);
        CreateSeatsTable(connection);
        CreateShowtimesTable(connection);
        CreateProductsTable(connection);

        // Các bảng đặt vé
        CreateBookingsTable(connection);
        CreateTicketsTable(connection);
        CreateBookingDetailProductsTable(connection);

        // Quản lý giá vé và khuyến mãi
        CreatePricingRulesTable(connection);
        CreatePromotionsTable(connection);

        // Tạo chỉ mục
        CreateIndexes(connection);
    }

    // =========================================
    // PROMOTIONS
    // =========================================

    private static void CreatePromotionsTable(SqliteConnection connection)
    {
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Promotions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Code TEXT NOT NULL COLLATE NOCASE UNIQUE,
                Name TEXT NOT NULL,
                DiscountType TEXT NOT NULL,
                DiscountValue NUMERIC NOT NULL DEFAULT 0,
                MaxDiscount NUMERIC NOT NULL DEFAULT 0,
                MinimumOrder NUMERIC NOT NULL DEFAULT 0,
                UsageLimit INTEGER NOT NULL DEFAULT 100,
                UsedCount INTEGER NOT NULL DEFAULT 0,
                StartDate TEXT NOT NULL,
                EndDate TEXT NOT NULL,
                IsEnabled INTEGER NOT NULL DEFAULT 1
            );
        ");

        AddColumnIfNotExists(
            connection,
            "Promotions",
            "UsageLimit",
            "INTEGER NOT NULL DEFAULT 100");

        AddColumnIfNotExists(
            connection,
            "Promotions",
            "UsedCount",
            "INTEGER NOT NULL DEFAULT 0");
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

        MigrateUsersSchemaIfNeeded(connection);

        connection.Execute(@"
            UPDATE Users
            SET CreatedAt = datetime('now', 'localtime')
            WHERE CreatedAt IS NULL OR CreatedAt = '';
        ");
    }

    /// <summary>
    /// Chuyển đổi bảng Users cũ (dùng khóa chính UserId hoặc Role kiểu TEXT)
    /// sang lược đồ chuẩn dùng khóa chính Id INTEGER PRIMARY KEY để khớp khóa ngoại với Bookings.
    /// </summary>
    private static void MigrateUsersSchemaIfNeeded(SqliteConnection connection)
    {
        bool hasIdColumn = false;
        bool hasPrimaryKeyId = false;
        bool hasLegacyUserId = false;
        bool isRoleInteger = false;

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "PRAGMA table_info(\"Users\");";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string colName = reader["name"]?.ToString() ?? "";
                string colType = reader["type"]?.ToString() ?? "";
                int pk = Convert.ToInt32(reader["pk"] ?? 0);

                if (colName.Equals("Id", StringComparison.OrdinalIgnoreCase))
                {
                    hasIdColumn = true;
                    hasPrimaryKeyId = pk > 0;
                }
                else if (colName.Equals("UserId", StringComparison.OrdinalIgnoreCase))
                {
                    hasLegacyUserId = true;
                }
                else if (colName.Equals("Role", StringComparison.OrdinalIgnoreCase))
                {
                    isRoleInteger = colType.Contains("INT", StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        if (hasPrimaryKeyId && !hasLegacyUserId && isRoleInteger)
            return;

        string idExpression = hasIdColumn
            ? "Id"
            : hasLegacyUserId
                ? "UserId"
                : "rowid";

        connection.Execute("PRAGMA foreign_keys = OFF;");
        try
        {
            using var tx = connection.BeginTransaction();

            connection.Execute($@"
                DROP TABLE IF EXISTS Users_new;

                CREATE TABLE Users_new (
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

                INSERT INTO Users_new (
                    Id, Username, PasswordHash, FullName, Role,
                    Email, Phone, RewardPoints, CreatedAt
                )
                SELECT
                    {idExpression},
                    Username,
                    COALESCE(PasswordHash, ''),
                    COALESCE(FullName, Username),
                    CASE
                        WHEN UPPER(TRIM(CAST(Role AS TEXT))) IN ('0', 'ADMIN') THEN 0
                        WHEN UPPER(TRIM(CAST(Role AS TEXT))) IN ('1', 'STAFF') THEN 1
                        WHEN UPPER(TRIM(CAST(Role AS TEXT))) IN ('2', 'CUSTOMER') THEN 2
                        ELSE 2
                    END,
                    COALESCE(Email, ''),
                    COALESCE(Phone, ''),
                    COALESCE(RewardPoints, 0),
                    COALESCE(NULLIF(CreatedAt, ''), datetime('now', 'localtime'))
                FROM Users;

                DROP TABLE Users;
                ALTER TABLE Users_new RENAME TO Users;
            ", transaction: tx);

            bool hasBookingsTable = connection.ExecuteScalar<bool>(@"
                SELECT EXISTS(
                    SELECT 1
                    FROM sqlite_master
                    WHERE type = 'table' AND name = 'Bookings'
                );
            ", transaction: tx);

            if (hasBookingsTable)
            {
                connection.Execute(@"
                    UPDATE Bookings
                    SET UserId = NULL
                    WHERE UserId IS NOT NULL
                      AND UserId NOT IN (SELECT Id FROM Users);
                ", transaction: tx);
            }

            tx.Commit();
        }
        finally
        {
            connection.Execute("PRAGMA foreign_keys = ON;");
        }
    }

    private static void SeedAdminUser(SqliteConnection connection)
    {
        connection.Execute(@"
            INSERT INTO Users (Username, PasswordHash, FullName, Role, CreatedAt)
            SELECT @Username, @PasswordHash, @FullName, @Role,
                   datetime('now', 'localtime')
            WHERE NOT EXISTS (
                SELECT 1
                FROM Users
                WHERE Username COLLATE NOCASE = @Username
            );
        ", new
        {
            Username = "admin",
            PasswordHash = "admin123",
            FullName = "Administrator",
            Role = (int)CinemaManagement.Models.Enums.UserRole.Admin
        });
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
    // BOOKINGS - ĐƠN ĐẶT VÉ
    // =========================================

    private static void CreateBookingsTable(SqliteConnection connection)
    {
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Bookings (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,

                UserId INTEGER NULL,
                BookingCode TEXT NOT NULL UNIQUE,

                BookingTime TEXT NOT NULL DEFAULT
                    (datetime('now', 'localtime')),

                TotalAmount NUMERIC NOT NULL DEFAULT 0,
                DiscountAmount NUMERIC NOT NULL DEFAULT 0,
                FinalAmount NUMERIC NOT NULL DEFAULT 0,

                PaymentMethod TEXT DEFAULT '',
                PaymentStatus TEXT NOT NULL DEFAULT 'PENDING',
                BookingStatus TEXT NOT NULL DEFAULT 'CONFIRMED',

                PromotionCode TEXT DEFAULT '',
                Channel TEXT DEFAULT 'Online',
                CustomerName TEXT DEFAULT '',

                FOREIGN KEY (UserId)
                    REFERENCES Users(Id)
                    ON DELETE SET NULL
            );
        ");

        AddColumnIfNotExists(
            connection,
            "Bookings",
            "Channel",
            "TEXT DEFAULT 'Online'");

        AddColumnIfNotExists(
            connection,
            "Bookings",
            "CustomerName",
            "TEXT DEFAULT ''");
    }

    // =========================================
    // TICKETS - VÉ ĐIỆN TỬ
    // =========================================

    private static void CreateTicketsTable(SqliteConnection connection)
    {
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Tickets (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,

                BookingId INTEGER NOT NULL,
                ShowtimeId INTEGER NOT NULL,
                SeatId INTEGER NOT NULL,

                TicketCode TEXT NOT NULL UNIQUE,
                SeatNumber TEXT NOT NULL,

                Price NUMERIC NOT NULL DEFAULT 0,
                Status TEXT NOT NULL DEFAULT 'BOOKED',

                CreatedAt TEXT NOT NULL DEFAULT
                    (datetime('now', 'localtime')),

                FOREIGN KEY (BookingId)
                    REFERENCES Bookings(Id)
                    ON DELETE CASCADE,

                FOREIGN KEY (ShowtimeId)
                    REFERENCES Showtimes(Id),

                FOREIGN KEY (SeatId)
                    REFERENCES Seats(Id)
            );
        ");

        MigrateTicketsUniqueConstraintIfNeeded(connection);
    }

    /// <summary>
    /// Nếu bảng Tickets cũ dùng UNIQUE (ShowtimeId, SeatId) cứng trên toàn bảng,
    /// chuyển sang bảng mới để kết hợp với partial unique index (bỏ qua vé CANCELLED).
    /// </summary>
    private static void MigrateTicketsUniqueConstraintIfNeeded(
        SqliteConnection connection)
    {
        string? createSql = connection.ExecuteScalar<string?>(@"
            SELECT sql
            FROM sqlite_master
            WHERE type = 'table' AND name = 'Tickets';
        ");

        bool hasTableLevelSeatUnique =
            !string.IsNullOrWhiteSpace(createSql) &&
            (createSql.Contains("UNIQUE (ShowtimeId, SeatId)", StringComparison.OrdinalIgnoreCase) ||
             createSql.Contains("UNIQUE(ShowtimeId, SeatId)", StringComparison.OrdinalIgnoreCase));

        if (!hasTableLevelSeatUnique)
            return;

        connection.Execute("PRAGMA foreign_keys = OFF;");
        try
        {
            using var tx = connection.BeginTransaction();

            connection.Execute(@"
                CREATE TABLE Tickets_new (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    BookingId INTEGER NOT NULL,
                    ShowtimeId INTEGER NOT NULL,
                    SeatId INTEGER NOT NULL,
                    TicketCode TEXT NOT NULL UNIQUE,
                    SeatNumber TEXT NOT NULL,
                    Price NUMERIC NOT NULL DEFAULT 0,
                    Status TEXT NOT NULL DEFAULT 'BOOKED',
                    CreatedAt TEXT NOT NULL DEFAULT (datetime('now', 'localtime')),
                    FOREIGN KEY (BookingId) REFERENCES Bookings(Id) ON DELETE CASCADE,
                    FOREIGN KEY (ShowtimeId) REFERENCES Showtimes(Id),
                    FOREIGN KEY (SeatId) REFERENCES Seats(Id)
                );

                INSERT INTO Tickets_new (
                    Id, BookingId, ShowtimeId, SeatId,
                    TicketCode, SeatNumber, Price, Status, CreatedAt
                )
                SELECT
                    Id, BookingId, ShowtimeId, SeatId,
                    TicketCode, SeatNumber, Price, Status, CreatedAt
                FROM Tickets;

                DROP TABLE Tickets;
                ALTER TABLE Tickets_new RENAME TO Tickets;
            ", transaction: tx);

            tx.Commit();
        }
        finally
        {
            connection.Execute("PRAGMA foreign_keys = ON;");
        }
    }

    // =========================================
    // BOOKING DETAIL PRODUCTS - BẮP NƯỚC
    // =========================================

    private static void CreateBookingDetailProductsTable(
        SqliteConnection connection)
    {
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS BookingDetailProducts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,

                BookingId INTEGER NOT NULL,
                ProductId INTEGER NULL,

                ProductName TEXT NOT NULL,
                Quantity INTEGER NOT NULL DEFAULT 1,

                UnitPrice NUMERIC NOT NULL DEFAULT 0,
                TotalPrice NUMERIC NOT NULL DEFAULT 0,

                FOREIGN KEY (BookingId)
                    REFERENCES Bookings(Id)
                    ON DELETE CASCADE,

                FOREIGN KEY (ProductId)
                    REFERENCES Products(Id)
                    ON DELETE SET NULL
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

            CREATE INDEX IF NOT EXISTS IX_Bookings_UserId
            ON Bookings(UserId);

            CREATE INDEX IF NOT EXISTS IX_Bookings_UserId_PromotionCode
            ON Bookings(UserId, PromotionCode);

            CREATE INDEX IF NOT EXISTS IX_Bookings_BookingTime
            ON Bookings(BookingTime);

            CREATE INDEX IF NOT EXISTS IX_Tickets_BookingId
            ON Tickets(BookingId);

            CREATE INDEX IF NOT EXISTS IX_Tickets_ShowtimeId
            ON Tickets(ShowtimeId);

            CREATE UNIQUE INDEX IF NOT EXISTS UX_Tickets_ActiveShowtimeSeat
            ON Tickets(ShowtimeId, SeatId)
            WHERE UPPER(Status) <> 'CANCELLED';

            CREATE INDEX IF NOT EXISTS IX_BookingDetailProducts_BookingId
            ON BookingDetailProducts(BookingId);
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