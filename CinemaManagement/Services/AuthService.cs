using System;
using CinemaManagement.Models;
using CinemaManagement.Models.Enums;

namespace CinemaManagement.Services;

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