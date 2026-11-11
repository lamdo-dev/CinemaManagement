
using System;
using System.Collections.Generic;
using System.Linq;
using CinemaManagement.Models;
using CinemaManagement.UserControls.Customer;

namespace CinemaManagement.Utils
{
    public static class UserSession
    {
        private static readonly object _bookingLock = new();

        // =====================================================
        // 1. USER SESSION
        // =====================================================

        public static User? CurrentUser { get; set; }

        public static bool IsLoggedIn
        {
            get
            {
                lock (_bookingLock)
                {
                    return CurrentUser != null;
                }
            }
        }

        public static void Login(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            lock (_bookingLock)
            {
                CurrentUser = user;
            }
        }

        // Đăng xuất không xóa lịch sử đặt vé.
        public static void Logout()
        {
            lock (_bookingLock)
            {
                CurrentUser = null;
            }
        }

        // =====================================================
        // 2. BOOKING DATA
        // =====================================================

        public static List<BookingResultEventArgs> BookingHistory { get; }
            = new();

        public static Dictionary<string, HashSet<string>>
            BookedSeatsByShowtime { get; } = new();

        public static BookingResultEventArgs? LatestBooking
        {
            get
            {
                lock (_bookingLock)
                {
                    return BookingHistory.LastOrDefault();
                }
            }
        }

        // =====================================================
        // 3. ADD BOOKING
        // =====================================================

        public static void AddBooking(BookingResultEventArgs booking)
        {
            if (booking == null)
                throw new ArgumentNullException(nameof(booking));

            if (string.IsNullOrWhiteSpace(booking.BookingCode))
                throw new InvalidOperationException(
                    "Mã đặt vé không được để trống.");

            if (booking.Movie == null)
                throw new InvalidOperationException(
                    "Đơn đặt vé chưa có thông tin phim.");

            if (booking.Showtime == null)
                throw new InvalidOperationException(
                    "Đơn đặt vé chưa có thông tin suất chiếu.");

            if (booking.Seats == null || booking.Seats.Count == 0)
                throw new InvalidOperationException(
                    "Đơn đặt vé phải có ít nhất một ghế.");

            lock (_bookingLock)
            {
                booking.BookingCode = booking.BookingCode.Trim();

                bool duplicatedBooking = BookingHistory.Any(x =>
                    string.Equals(
                        x.BookingCode,
                        booking.BookingCode,
                        StringComparison.OrdinalIgnoreCase));

                if (duplicatedBooking)
                    throw new InvalidOperationException(
                        $"Mã đặt vé {booking.BookingCode} đã tồn tại.");

                string showtimeKey = GetShowtimeKey(
                    booking.Movie,
                    booking.Showtime);

                if (!BookedSeatsByShowtime.TryGetValue(
                    showtimeKey, out var bookedSeats))
                {
                    bookedSeats = new HashSet<string>(
                        StringComparer.OrdinalIgnoreCase);

                    BookedSeatsByShowtime[showtimeKey] = bookedSeats;
                }

                var selectedSeats = booking.Seats
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim().ToUpperInvariant())
                    .ToList();

                if (selectedSeats.Count == 0)
                    throw new InvalidOperationException(
                        "Danh sách ghế không hợp lệ.");

                if (selectedSeats.Count != selectedSeats
                    .Distinct(StringComparer.OrdinalIgnoreCase).Count())
                {
                    throw new InvalidOperationException(
                        "Danh sách ghế có ghế bị trùng.");
                }

                foreach (string seat in selectedSeats)
                {
                    if (bookedSeats.Contains(seat))
                        throw new InvalidOperationException(
                            $"Ghế {seat} đã được đặt trước đó.");
                }

                booking.Seats = selectedSeats;

                BookingHistory.Add(booking);

                foreach (string seat in selectedSeats)
                {
                    bookedSeats.Add(seat);
                }
            }
        }

        // =====================================================
        // 4. GET BOOKED SEATS
        // =====================================================

        public static HashSet<string> GetBookedSeats(
            Movie? movie,
            Showtime? showtime)
        {
            string key = GetShowtimeKey(movie, showtime);

            lock (_bookingLock)
            {
                if (BookedSeatsByShowtime.TryGetValue(
                    key, out var seats))
                {
                    return new HashSet<string>(
                        seats,
                        StringComparer.OrdinalIgnoreCase);
                }

                return new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);
            }
        }

        // =====================================================
        // 5. SHOWTIME KEY
        // =====================================================

        private static string GetShowtimeKey(
            Movie? movie,
            Showtime? showtime)
        {
            if (movie == null || showtime == null)
                throw new InvalidOperationException(
                    "Chưa có thông tin phim hoặc suất chiếu.");

            return $"{movie.Id}_{showtime.RoomId}_" +
                   $"{showtime.StartTime:yyyyMMdd_HHmmss}";
        }

        // =====================================================
        // 6. FIND BOOKING
        // =====================================================

        public static BookingResultEventArgs? FindBooking(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            lock (_bookingLock)
            {
                return BookingHistory.LastOrDefault(x =>
                    string.Equals(
                        x.BookingCode,
                        code.Trim(),
                        StringComparison.OrdinalIgnoreCase));
            }
        }

        // =====================================================
        // 7. CHECK-IN DATA
        // =====================================================

        public static Dictionary<string, DateTime> CheckedInBookings { get; }
            = new(StringComparer.OrdinalIgnoreCase);

        public static List<TicketScanResult> ScanLogs { get; }
            = new();

        public static bool IsCheckedIn(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            lock (_bookingLock)
            {
                return CheckedInBookings.ContainsKey(code.Trim());
            }
        }

        // =====================================================
        // 8. SCAN TICKET
        // =====================================================

        public static TicketScanResult ScanTicket(string code)
        {
            string normalized = (code ?? string.Empty).Trim();

            lock (_bookingLock)
            {
                var result = new TicketScanResult
                {
                    Code = normalized,
                    ScanTime = DateTime.Now
                };

                if (string.IsNullOrWhiteSpace(normalized))
                {
                    result.Status = TicketScanStatus.NotFound;
                    result.Message = "Vui lòng nhập mã vé.";
                }
                else
                {
                    var booking = BookingHistory.LastOrDefault(x =>
                        string.Equals(
                            x.BookingCode,
                            normalized,
                            StringComparison.OrdinalIgnoreCase));

                    result.Booking = booking;

                    if (booking == null)
                    {
                        result.Status = TicketScanStatus.NotFound;
                        result.Message = "Mã vé không tồn tại!";
                    }
                    else if (CheckedInBookings.TryGetValue(
                        normalized, out DateTime firstTime))
                    {
                        result.Status = TicketScanStatus.AlreadyUsed;
                        result.FirstCheckInTime = firstTime;

                        result.Message =
                            $"Vé đã được sử dụng lúc " +
                            $"{firstTime:HH:mm:ss dd/MM/yyyy}";
                    }
                    else if (booking.Showtime != null &&
                             result.ScanTime > booking.Showtime.EndTime)
                    {
                        result.Status = TicketScanStatus.Expired;
                        result.Message =
                            "Vé đã hết hạn vì suất chiếu đã kết thúc.";
                    }
                    else
                    {
                        CheckedInBookings[normalized] = result.ScanTime;

                        result.Status = TicketScanStatus.Valid;
                        result.Message =
                            "Vé hợp lệ. Mời khách vào phòng chiếu.";
                    }
                }

                ScanLogs.Add(result);

                return result;
            }
        }

        // =====================================================
        // 9. CLEAR SESSION
        // =====================================================

        // Chỉ gọi khi muốn xóa toàn bộ dữ liệu phiên.
        // Không gọi khi đăng xuất thông thường.
        public static void ClearSession()
        {
            lock (_bookingLock)
            {
                CurrentUser = null;

                BookingHistory.Clear();
                BookedSeatsByShowtime.Clear();
                CheckedInBookings.Clear();
                ScanLogs.Clear();
            }
        }
    }

    // =========================================================
    // 10. TICKET SCAN STATUS
    // =========================================================

    public enum TicketScanStatus
    {
        Valid,
        AlreadyUsed,
        Expired,
        NotFound
    }

    // =========================================================
    // 11. TICKET SCAN RESULT
    // =========================================================

    public class TicketScanResult : EventArgs
    {
        public string Code { get; set; } = string.Empty;

        public TicketScanStatus Status { get; set; }

        public BookingResultEventArgs? Booking { get; set; }

        public DateTime ScanTime { get; set; }

        public DateTime? FirstCheckInTime { get; set; }

        public string Message { get; set; } = string.Empty;

        public bool IsValid =>
            Status == TicketScanStatus.Valid;
    }
}
