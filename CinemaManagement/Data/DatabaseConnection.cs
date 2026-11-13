using Microsoft.Data.Sqlite;

namespace CinemaManagement.Data;

public static class DatabaseConnection
{
    private const string ConnectionString =
        "Data Source=cinema.db;Foreign Keys=True";

    public static SqliteConnection GetConnection()
    {
        return new SqliteConnection(ConnectionString);
    }
}