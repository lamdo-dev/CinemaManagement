
using System;
using CinemaManagement.Models;
using CinemaManagement.Models.Enums;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CinemaManagement.Services;

public class AuthService
{
    private const string ConnectionString = "Data Source=cinema.db";

    /// <summary>
    /// Xử lý đăng nhập
    /// </summary>
    public User? Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        try
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var user = connection.QueryFirstOrDefault<User>(
                @"SELECT * FROM Users
                  WHERE LOWER(Username) = LOWER(@Username)",
                new { Username = username });

            if (user != null && user.PasswordHash == password)
            {
                return user;
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Xử lý đăng ký tài khoản mới
    /// </summary>
    public (bool Success, string Message, User? CreatedUser) Register(
        string username,
        string password,
        string fullName)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(fullName))
        {
            return (false, "Vui lòng nhập đầy đủ thông tin!", null);
        }

        try
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var existing = connection.QueryFirstOrDefault<User>(
                @"SELECT * FROM Users
                  WHERE LOWER(Username) = LOWER(@Username)",
                new { Username = username });

            if (existing != null)
            {
                return (
                    false,
                    "Tên đăng nhập này đã tồn tại! Vui lòng chọn tên khác.",
                    null
                );
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

            return (
                true,
                "Đăng ký tài khoản thành công!",
                newUser
            );
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi khi đăng ký: {ex.Message}", null);
        }
    }
}
