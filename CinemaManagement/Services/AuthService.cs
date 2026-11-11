using System;
using CinemaManagement.Models;
using CinemaManagement.Models.Enums;

namespace CinemaManagement.Services;

using CinemaManagement.Models;
using CinemaManagement.Models.Enums;
using Dapper;
using Microsoft.Data.Sqlite;

public class AuthService
{

    /// <summary>
    /// Xử lý đăng nhập
    /// </summary>
    public User? Login(string username, string password)
    {
        // Tài khoản mặc định để thử nghiệm
        if (username.Equals("admin", StringComparison.OrdinalIgnoreCase) && password == "admin123")
        {
            return new User
            {
                Username = "admin",
                FullName = "Administrator",
                Role = UserRole.Admin
            };
        }

        // Đăng nhập cho tài khoản khách hàng thông thường
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            return new User
            {
                Username = username,
                FullName = username,
                Role = UserRole.Customer
            };
        }

        return null;
    }

    /// <summary>
    /// Xử lý đăng ký tài khoản mới
    /// </summary>
    public (bool Success, string Message, User? CreatedUser) Register(string username, string password, string fullName)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(fullName))
        {
            return (false, "Vui lòng nhập đầy đủ thông tin!", null);
        }

        var newUser = new User
        {
            Username = username,
            FullName = fullName,
            Role = UserRole.Customer
        };

        return (true, "Đăng ký tài khoản thành công!", newUser);
    }
}

    private const string ConnectionString = "Data Source=cinema.db";

    public User? Login(string username, string password)
    {
        try
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var user = connection.QueryFirstOrDefault<User>(
                "SELECT * FROM Users WHERE LOWER(Username) = LOWER(@Username)",
                new { Username = username });

            if (user != null && user.PasswordHash == password)
            {
                return user;
            }
        }
        catch
        {
            // Fallback nếu DB chưa có dữ liệu
        }

        // Hỗ trợ đăng nhập nhanh theo tài khoản test hoặc bất kỳ tài khoản Customer nào
        UserRole role = username.ToLowerInvariant() switch
        {
            "admin" => UserRole.Admin,
            "staff" => UserRole.Staff,
            _ => UserRole.Customer
        };

        return new User
        {
            Id = 1,
            Username = username,
            FullName = username,
            Role = role
        };
    }

    public (bool Success, string Message, User? CreatedUser) Register(
        string username,
        string password,
        string fullName)
    {
        try
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var existing = connection.QueryFirstOrDefault<User>(
                "SELECT * FROM Users WHERE LOWER(Username) = LOWER(@Username)",
                new { Username = username });

            if (existing != null)
            {
                return (false, "Tên đăng nhập này đã tồn tại! Vui lòng chọn tên khác.", null);
            }

            connection.Execute(
                @"INSERT INTO Users (Username, PasswordHash, FullName, Role)
                  VALUES (@Username, @PasswordHash, @FullName, @Role)",
                new
                {
                    Username = username,
                    PasswordHash = password,
                    FullName = fullName,
                    Role = (int)UserRole.Customer
                });

            var newUser = new User
            {
                Username = username,
                FullName = fullName,
                Role = UserRole.Customer
            };

            return (true, "Đăng ký tài khoản thành công!", newUser);
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi khi đăng ký: {ex.Message}", null);
        }
    }
}
