using System;
using System.Collections.Generic;
using System.Linq;
using CinemaManagement.Models;
using CinemaManagement.UserControls.Customer;

namespace CinemaManagement.Utils;

public static class UserSession
{
    public static User? CurrentUser { get; set; }

    public static bool IsLoggedIn => CurrentUser != null;

    // Lưu lịch sử các đơn đặt vé thành công (cả Online lẫn bán tại quầy POS)
    public static List<BookingResultEventArgs> BookingHistory { get; } = new();

    // Lưu danh sách mã ghế đã bán theo khóa suất chiếu (MovieId_StartTime)
    public static Dictionary<string, HashSet<string>> BookedSeatsByShowtime { get; } = new();

    public static void Clear()
    {
        CurrentUser = null;
    }
}