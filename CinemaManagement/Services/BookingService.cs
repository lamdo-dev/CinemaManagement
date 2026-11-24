using System;
using CinemaManagement.Models;
using CinemaManagement.Repositories;
using CinemaManagement.UserControls.Customer;
using CinemaManagement.Utils;

namespace CinemaManagement.Services
{
    public class BookingService
    {
        private readonly BookingRepository _bookingRepository;

        public BookingService()
        {
            _bookingRepository = new BookingRepository();
        }

        /// <summary>
        /// Lưu đơn đặt vé của tài khoản hiện tại.
        /// Nếu hệ thống cho phép khách vãng lai, UserId sẽ được lưu là NULL.
        /// </summary>
        public long CreateBooking(BookingResultEventArgs booking)
        {
            ValidateBooking(booking);

            User? currentUser = UserSession.CurrentUser;
            int? userId = currentUser?.Id;

            NormalizeBookingTime(booking);

            long bookingId = _bookingRepository.SaveBooking(booking, userId);

            if (bookingId <= 0)
            {
                throw new InvalidOperationException(
                    "Không thể lưu đơn đặt vé vào cơ sở dữ liệu.");
            }

            return bookingId;
        }

        /// <summary>
        /// Lưu đơn đặt vé cho khách chưa đăng nhập.
        /// </summary>
        public long CreateGuestBooking(BookingResultEventArgs booking)
        {
            ValidateBooking(booking);

            if (UserSession.CurrentUser != null)
            {
                throw new InvalidOperationException(
                    "Người dùng đã đăng nhập. Hãy sử dụng CreateBooking.");
            }

            NormalizeBookingTime(booking);

            long bookingId = _bookingRepository.SaveBooking(booking, null);

            if (bookingId <= 0)
            {
                throw new InvalidOperationException(
                    "Không thể lưu đơn đặt vé của khách vãng lai.");
            }

            return bookingId;
        }

        private static void ValidateBooking(BookingResultEventArgs booking)
        {
            if (booking == null)
                throw new ArgumentNullException(nameof(booking));

            if (booking.Movie == null || booking.Movie.Id <= 0)
                throw new InvalidOperationException(
                    "Chưa có thông tin phim hợp lệ.");

            if (booking.Showtime == null || booking.Showtime.Id <= 0)
                throw new InvalidOperationException(
                    "Chưa có thông tin suất chiếu hợp lệ.");

            if (booking.Seats == null || booking.Seats.Count == 0)
                throw new InvalidOperationException(
                    "Chưa có ghế được chọn.");

            if (string.IsNullOrWhiteSpace(booking.BookingCode))
                throw new InvalidOperationException(
                    "Mã đặt vé không hợp lệ.");

            if (booking.TotalAmount < 0 ||
                booking.DiscountAmount < 0 ||
                booking.TicketAmount < 0 ||
                booking.FoodAmount < 0)
            {
                throw new InvalidOperationException(
                    "Thông tin thanh toán không hợp lệ.");
            }

            if (booking.FoodCart == null)
                throw new InvalidOperationException(
                    "Giỏ hàng bắp nước không hợp lệ.");
        }

        private static void NormalizeBookingTime(BookingResultEventArgs booking)
        {
            if (booking.BookingTime == default)
                booking.BookingTime = DateTime.Now;
        }
    }
}