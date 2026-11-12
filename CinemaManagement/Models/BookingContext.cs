
using System;
using System.Collections.Generic;
using System.Linq;

namespace CinemaManagement.Models
{
    /// <summary>
    /// Quản lý dữ liệu xuyên suốt quá trình đặt vé.
    /// Các màn hình sử dụng chung một BookingContext.
    /// </summary>
    public sealed class BookingContext
    {
        // Phim được chọn
        public Movie? Movie { get; private set; }

        // Suất chiếu được chọn
        public Showtime? Showtime { get; private set; }

        // Danh sách ghế
        public List<string> Seats { get; private set; } = new();

        // Tổng tiền vé
        public decimal TicketAmount { get; private set; }

        // Giỏ hàng bắp nước
        public Dictionary<int, (
            string Name,
            decimal Price,
            int Qty,
            string ImgPath)> FoodCart { get; private set; } = new();

        // Tổng tiền bắp nước
        public decimal FoodAmount { get; private set; }

        // Tổng tiền đặt vé
        public decimal TotalAmount => TicketAmount + FoodAmount;

        // Kiểm tra phim và suất chiếu
        public bool HasShowtime =>
            Movie != null && Showtime != null;

        // Kiểm tra ghế
        public bool HasSeats =>
            HasShowtime &&
            Seats.Count > 0;

        // Kiểm tra có thể thanh toán
        public bool CanCheckout =>
            HasSeats &&
            TicketAmount > 0 &&
            Seats.Count == Seats.Distinct().Count();

        // Xác định hai suất chiếu có giống nhau không
        private static bool IsSameShowtime(
            Showtime? first,
            Showtime? second)
        {
            if (first == null || second == null)
                return false;

            // Ưu tiên so sánh ID nếu cả hai có ID hợp lệ
            if (first.Id > 0 && second.Id > 0)
                return first.Id == second.Id;

            // Nếu không có ID hợp lệ, so sánh thông tin suất chiếu
            return first.RoomId == second.RoomId &&
                   first.StartTime == second.StartTime;
        }

        // Cập nhật phim
        public void SetMovie(Movie movie)
        {
            if (movie == null)
                throw new ArgumentNullException(nameof(movie));

            bool changed = Movie == null || Movie.Id != movie.Id;

            Movie = movie;

            if (changed)
            {
                ClearAfterMovieChange();
            }
        }

        // Cập nhật suất chiếu
        public void SetShowtime(Showtime showtime)
        {
            if (showtime == null)
                throw new ArgumentNullException(nameof(showtime));

            bool changed = !IsSameShowtime(Showtime, showtime);

            Showtime = showtime;

            if (changed)
            {
                ClearAfterShowtimeChange();
            }
        }

        // Cập nhật danh sách ghế
        public void SetSeats(IEnumerable<string>? seats)
        {
            Seats = seats?
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().ToUpperInvariant())
                .Distinct()
                .ToList() ?? new List<string>();

            // Không còn ghế thì không còn tiền vé
            if (Seats.Count == 0)
            {
                TicketAmount = 0m;
            }
        }

        // Cập nhật tiền vé
        public void SetTicketAmount(decimal amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            TicketAmount = amount;
        }

        // Cập nhật giỏ hàng
        public void SetFoodCart(
            Dictionary<int, (
                string Name,
                decimal Price,
                int Qty,
                string ImgPath)>? cart)
        {
            var newCart = new Dictionary<int, (
                string Name,
                decimal Price,
                int Qty,
                string ImgPath)>();

            if (cart != null)
            {
                foreach (var item in cart)
                {
                    if (item.Key <= 0 ||
                        item.Value.Qty <= 0 ||
                        item.Value.Price < 0)
                    {
                        continue;
                    }

                    newCart[item.Key] = item.Value;
                }
            }

            FoodCart = newCart;

            RecalculateFoodAmount();
        }

        // Tính tiền bắp nước
        public void RecalculateFoodAmount()
        {
            FoodAmount = FoodCart.Values.Sum(
                item => item.Price * item.Qty);
        }

        // Xóa dữ liệu phụ thuộc khi đổi phim
        public void ClearAfterMovieChange()
        {
            Showtime = null;

            ClearAfterShowtimeChange();
        }

        // Xóa dữ liệu phụ thuộc khi đổi suất chiếu
        public void ClearAfterShowtimeChange()
        {
            Seats.Clear();
            TicketAmount = 0m;

            FoodCart.Clear();
            FoodAmount = 0m;
        }

        // Xóa toàn bộ quy trình đặt vé
        public void Clear()
        {
            Movie = null;
            Showtime = null;

            ClearAfterShowtimeChange();
        }
    }
}
