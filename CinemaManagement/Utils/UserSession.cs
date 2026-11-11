namespace CinemaManagement.Utils;

using System;
using System.Collections.Generic;
using System.Linq;
using CinemaManagement.Models;
using CinemaManagement.UserControls.Customer;

public static class UserSession
{
    public static User? CurrentUser { get; set; }

    // Lưu lịch sử các đơn đặt vé thành công (cả Online lẫn bán tại quầy POS)
    public static List<BookingResultEventArgs> BookingHistory { get; } = new();

    // Lưu danh sách mã ghế đã bán theo khóa suất chiếu (MovieId_StartTime)
    public static Dictionary<string, HashSet<string>> BookedSeatsByShowtime { get; } = new();

    // Lưu các đơn đã soát vé vào phòng: Mã đơn -> Thời điểm soát
    public static Dictionary<string, DateTime> CheckedInBookings { get; } = new(StringComparer.OrdinalIgnoreCase);

    // Nhật ký soát vé
    public static List<TicketScanResult> ScanLogs { get; } = new();

    public static BookingResultEventArgs? LatestBooking =>
        BookingHistory.Count > 0 ? BookingHistory[^1] : null;

    public static void AddBooking(BookingResultEventArgs booking)
    {
        BookingHistory.Add(booking);

        string key = GetShowtimeKey(booking.Movie, booking.Showtime);
        if (!BookedSeatsByShowtime.TryGetValue(key, out var seatSet))
        {
            seatSet = new HashSet<string>();
            BookedSeatsByShowtime[key] = seatSet;
        }

        foreach (var seat in booking.Seats)
        {
            seatSet.Add(seat);
        }
    }

    public static HashSet<string> GetBookedSeats(Movie? movie, Showtime? showtime)
    {
        string key = GetShowtimeKey(movie, showtime);
        return BookedSeatsByShowtime.TryGetValue(key, out var seatSet)
            ? new HashSet<string>(seatSet)
            : new HashSet<string>();
    }

    private static string GetShowtimeKey(Movie? movie, Showtime? showtime)
    {
        int movieId = movie?.Id ?? 1;
        string timeKey = showtime?.StartTime.ToString("yyyyMMdd_HHmm") ?? "default";
        return $"{movieId}_{timeKey}";
    }

    // =========================================================
    // SOÁT VÉ VÀO PHÒNG CHIẾU
    // =========================================================
    public static BookingResultEventArgs? FindBooking(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        string normalized = code.Trim();
        return BookingHistory.LastOrDefault(b =>
            string.Equals(b.BookingCode, normalized, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsCheckedIn(string code) => CheckedInBookings.ContainsKey(code.Trim());

    public static TicketScanResult ScanTicket(string code)
    {
        string normalized = (code ?? string.Empty).Trim();
        var result = new TicketScanResult
        {
            Code = normalized,
            ScanTime = DateTime.Now
        };

        var booking = FindBooking(normalized);
        result.Booking = booking;

        if (booking == null)
        {
            result.Status = TicketScanStatus.NotFound;
            result.Message = "Mã vé không tồn tại trong hệ thống!";
        }
        else if (CheckedInBookings.TryGetValue(normalized, out var firstTime))
        {
            result.Status = TicketScanStatus.AlreadyUsed;
            result.FirstCheckInTime = firstTime;
            result.Message = $"Vé đã được soát lúc {firstTime:HH:mm:ss dd/MM}. Cảnh báo vé trùng!";
        }
        else if (IsShowtimeExpired(booking))
        {
            result.Status = TicketScanStatus.Expired;
            result.Message = "Suất chiếu của vé này đã kết thúc!";
        }
        else
        {
            CheckedInBookings[normalized] = result.ScanTime;
            result.Status = TicketScanStatus.Valid;
            result.Message = "Vé hợp lệ - Mời khách vào phòng chiếu.";
        }

        ScanLogs.Add(result);
        return result;
    }

    private static bool IsShowtimeExpired(BookingResultEventArgs booking)
    {
        if (booking.Showtime == null || booking.Showtime.StartTime == default) return false;
        int duration = booking.Movie?.DurationMinutes > 0 ? booking.Movie.DurationMinutes : 120;
        return booking.Showtime.StartTime.AddMinutes(duration + 30) < DateTime.Now;
    }
}

public enum TicketScanStatus
{
    Valid,        // Hợp lệ - cho vào
    AlreadyUsed,  // Vé đã soát (trùng)
    Expired,      // Suất chiếu đã kết thúc
    NotFound      // Mã không tồn tại
}

public class TicketScanResult : EventArgs
{
    public string Code { get; set; } = string.Empty;
    public TicketScanStatus Status { get; set; }
    public BookingResultEventArgs? Booking { get; set; }
    public DateTime ScanTime { get; set; }
    public DateTime? FirstCheckInTime { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsValid => Status == TicketScanStatus.Valid;
}
