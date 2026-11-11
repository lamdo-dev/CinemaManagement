using CinemaManagement.Models;

namespace CinemaManagement.Utils;

public static class UserSession
{
    public static User? CurrentUser { get; set; }

    public static bool IsLoggedIn => CurrentUser != null;

    public static void Clear()
    {
        CurrentUser = null;
    }
}