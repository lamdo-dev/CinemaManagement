using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Data;

namespace CinemaManagement;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (args.Any(a => string.Equals(a, "--reset-seed", StringComparison.OrdinalIgnoreCase)))
        {
            DatabaseInitializer.Initialize();
            using var connection = DatabaseConnection.GetConnection();
            connection.Open();
            DatabaseSeeder.SeedAll(connection, resetExisting: true);

            int exportIdx = Array.FindIndex(
                args,
                a => string.Equals(a, "--export-sql", StringComparison.OrdinalIgnoreCase));

            if (exportIdx >= 0 && exportIdx + 1 < args.Length)
            {
                DatabaseSeeder.ExportDatabaseToSqlFile(connection, Path.GetFullPath(args[exportIdx + 1]));
            }

            return;
        }

        ApplicationConfiguration.Initialize();

        DatabaseInitializer.Initialize();

        // Khởi động vào Form Đăng nhập trước
        Application.Run(new LoginForm());
    }
}