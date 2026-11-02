namespace CinemaManagement.Models;

using CinemaManagement.Models.Enums;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Customer;
    public int RewardPoints { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}