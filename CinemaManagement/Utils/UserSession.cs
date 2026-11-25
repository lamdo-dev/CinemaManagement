
using System;
using System.Collections.Generic;
using System.Linq;
using CinemaManagement.Models;
using CinemaManagement.Models.Enums;
using CinemaManagement.Repositories;

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
                    if (CurrentUser != null &&
                        CurrentUser.Role == UserRole.Customer)
                    {
                        return BookingHistory.LastOrDefault(
                            x => x.UserId == CurrentUser.Id);
                    }

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
                booking.UserId ??= CurrentUser?.Id;

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

            string trimmed = code.Trim();

            lock (_bookingLock)
            {
                var inMemory = BookingHistory.LastOrDefault(x =>
                    string.Equals(
                        x.BookingCode,
                        trimmed,
                        StringComparison.OrdinalIgnoreCase));

                if (inMemory != null)
                    return inMemory;
            }

            try
            {
                return new BookingRepository().GetBookingByCode(trimmed);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Hợp nhất danh sách vé từ SQLite và các đơn POS trong phiên làm việc
        /// để màn hình soát vé luôn thấy đầy đủ dữ liệu kể cả sau khi mở lại ứng dụng.
        /// </summary>
        public static List<BookingResultEventArgs> GetAllTicketBookings()
        {
            var merged = new List<BookingResultEventArgs>();
            var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            lock (_bookingLock)
            {
                for (int i = BookingHistory.Count - 1; i >= 0; i--)
                {
                    var b = BookingHistory[i];
                    if (b.Seats != null &&
                        b.Seats.Count > 0 &&
                        !string.IsNullOrWhiteSpace(b.BookingCode) &&
                        seenCodes.Add(b.BookingCode))
                    {
                        merged.Add(b);
                    }
                }
            }

            try
            {
                var dbBookings = new BookingRepository().GetAllTicketBookings();
                foreach (var b in dbBookings)
                {
                    if (!string.IsNullOrWhiteSpace(b.BookingCode) &&
                        seenCodes.Add(b.BookingCode))
                    {
                        merged.Add(b);
                    }
                }
            }
            catch
            {
                // Vẫn trả về danh sách trong bộ nhớ nếu DB tạm thời không đọc được
            }

            return merged;
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

            string trimmed = code.Trim();

            lock (_bookingLock)
            {
                if (CheckedInBookings.ContainsKey(trimmed))
                    return true;
            }

            try
            {
                return new BookingRepository().IsBookingCheckedIn(trimmed);
            }
            catch
            {
                return false;
            }
        }

        // =====================================================
        // 8. SCAN TICKET
        // =====================================================

        public static TicketScanResult ScanTicket(string code, int? expectedShowtimeId = null)
        {
            User? currentUser = CurrentUser;
            if (currentUser == null || currentUser.Id <= 0 ||
                (currentUser.Role != UserRole.Staff && currentUser.Role != UserRole.Admin))
            {
                throw new InvalidOperationException(
                    "Chỉ nhân viên hoặc quản trị viên mới được soát vé.");
            }

            int staffUserId = currentUser.Id;
            string normalized = (code ?? string.Empty).Trim();
            var repo = new BookingRepository();

            lock (_bookingLock)
            {
                var result = new TicketScanResult
                {
                    Code = normalized.ToUpperInvariant(),
                    ScanTime = DateTime.Now
                };

                if (string.IsNullOrWhiteSpace(normalized))
                {
                    result.Status = TicketScanStatus.NotFound;
                    result.Message = "Vui lòng nhập mã vé.";
                }
                else
                {
                    BookingResultEventArgs? booking = repo.GetBookingByCode(normalized);

                    result.Booking = booking;

                    if (booking == null)
                    {
                        result.Status = TicketScanStatus.NotFound;
                        result.Message = "Mã vé không tồn tại!";
                    }
                    else if (booking.Seats == null || booking.Seats.Count == 0 || booking.Showtime == null)
                    {
                        result.Status = TicketScanStatus.NotFound;
                        result.Message = "Mã giao dịch này không có vé xem phim.";
                    }
                    else if (!string.Equals(booking.PaymentStatus, "PAID", StringComparison.OrdinalIgnoreCase))
                    {
                        result.Status = TicketScanStatus.NotFound;
                        result.Message = "Vé chưa được thanh toán.";
                    }
                    else if (expectedShowtimeId.HasValue &&
                             booking.Showtime.Id != expectedShowtimeId.Value)
                    {
                        result.Status = TicketScanStatus.WrongShowtime;
                        result.Message = "Vé không đúng suất chiếu đang được kiểm tra.";
                    }
                    else if (repo.IsBookingCheckedIn(normalized))
                    {
                        result.Status = TicketScanStatus.AlreadyUsed;
                        if (!CheckedInBookings.TryGetValue(normalized, out DateTime firstTime))
                            firstTime = repo.GetBookingCheckInTime(normalized) ?? default;

                        result.FirstCheckInTime = firstTime == default ? null : firstTime;
                        result.Message = firstTime == default
                            ? "Vé đã được soát trước đó."
                            : $"Vé đã được sử dụng lúc {firstTime:HH:mm:ss dd/MM/yyyy}";
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
                        if (repo.MarkBookingCheckedIn(normalized, staffUserId, result.ScanTime))
                        {
                            CheckedInBookings[normalized] = result.ScanTime;
                            result.Status = TicketScanStatus.Valid;
                            result.Message = "Vé hợp lệ. Mời khách vào phòng chiếu.";
                        }
                        else if (repo.IsBookingCheckedIn(normalized))
                        {
                            result.Status = TicketScanStatus.AlreadyUsed;
                            result.Message = "Vé vừa được soát bởi nhân viên khác.";
                        }
                        else
                        {
                            result.Status = TicketScanStatus.NotFound;
                            result.Message = "Vé không còn hợp lệ hoặc đã bị hủy.";
                        }
                    }
                }

                if (result.Status != TicketScanStatus.Valid)
                    repo.SaveTicketScanLog(result, staffUserId);

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
        WrongShowtime,
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
