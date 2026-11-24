using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using CinemaManagement.Data;
using CinemaManagement.Models;
using CinemaManagement.UserControls.Customer;

namespace CinemaManagement.Repositories
{
    public class BookingRepository
    {
        /// <summary>
        /// Lưu toàn bộ đơn đặt vé, ghế và bắp nước.
        /// Tất cả thao tác được thực hiện trong một transaction.
        /// </summary>
        public long SaveBooking(
            BookingResultEventArgs booking,
            int? userId = null)
        {
            if (booking == null)
                throw new ArgumentNullException(nameof(booking));

            ValidateBooking(booking);

            using var connection = DatabaseConnection.GetConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Kiểm tra suất chiếu
                var showtime = connection.QueryFirstOrDefault<Showtime>(@"
                    SELECT Id, MovieId, RoomId, StartTime, EndTime,
                           BasePrice, Format
                    FROM Showtimes
                    WHERE Id = @ShowtimeId
                      AND MovieId = @MovieId;
                ",
                new
                {
                    ShowtimeId = booking.Showtime!.Id,
                    MovieId = booking.Movie!.Id
                }, transaction);

                if (showtime == null)
                    throw new InvalidOperationException(
                        "Suất chiếu không tồn tại hoặc đã bị thay đổi.");

                if (showtime.StartTime <= DateTime.Now)
                    throw new InvalidOperationException(
                        "Suất chiếu đã bắt đầu hoặc đã qua giờ chiếu. Không thể đặt vé.");

                // 2. Kiểm tra tài khoản
                if (userId.HasValue)
                {
                    bool userExists = connection.ExecuteScalar<bool>(@"
                        SELECT EXISTS(
                            SELECT 1 FROM Users WHERE Id = @UserId
                        );
                    ", new { UserId = userId.Value }, transaction);

                    if (!userExists)
                        throw new InvalidOperationException(
                            "Tài khoản người đặt không tồn tại.");
                }

                // 3. Lấy thông tin ghế từ database
                var seats = connection.Query<Seat>(@"
                    SELECT Id, RoomId, SeatNumber,
                           RowIndex, ColumnIndex, Type, PriceMultiplier
                    FROM Seats
                    WHERE RoomId = @RoomId
                      AND SeatNumber IN @SeatNumbers;
                ",
                new
                {
                    RoomId = showtime.RoomId,
                    SeatNumbers = booking.Seats
                }, transaction).ToList();

                if (seats.Count != booking.Seats.Count)
                    throw new InvalidOperationException(
                        "Một hoặc nhiều ghế không tồn tại trong phòng chiếu.");

                // Kiểm tra ghế đã có người đặt trong cùng transaction
                var alreadyBookedSeats = connection.Query<string>(@"
                    SELECT SeatNumber
                    FROM Tickets
                    WHERE ShowtimeId = @ShowtimeId
                      AND SeatId IN @SeatIds
                      AND UPPER(Status) IN ('BOOKED', 'CONFIRMED', 'USED');
                ",
                new
                {
                    ShowtimeId = showtime.Id,
                    SeatIds = seats.Select(s => s.Id).ToList()
                }, transaction).ToList();

                if (alreadyBookedSeats.Count > 0)
                {
                    throw new InvalidOperationException(
                        $"Ghế {string.Join(", ", alreadyBookedSeats)} đã được người khác đặt. " +
                        "Vui lòng chọn lại ghế khác.");
                }

                // 4. Sử dụng số tiền gốc đã được CheckoutUC tính toán.
                // TicketAmount và FoodAmount là giá trị trước khuyến mãi.
                decimal ticketAmount = booking.TicketAmount;
                decimal foodAmount = booking.FoodAmount;

                decimal grossAmount = ticketAmount + foodAmount;
                decimal expectedFinalAmount = grossAmount - booking.DiscountAmount;

                if (ticketAmount < 0 || foodAmount < 0 ||
                    booking.DiscountAmount < 0 ||
                    expectedFinalAmount < 0)
                {
                    throw new InvalidOperationException(
                        "Thông tin tiền vé, bắp nước hoặc khuyến mãi không hợp lệ.");
                }

                if (Math.Abs(expectedFinalAmount - booking.TotalAmount) > 0.01m)
                {
                    throw new InvalidOperationException(
                        "Tổng tiền thanh toán không khớp với chi tiết đơn hàng. " +
                        "Vui lòng quay lại màn hình thanh toán.");
                }

                // 5. Tính giá từng ghế theo hệ số ghế
                decimal totalWeight = (decimal)seats.Sum(
                    seat => seat.PriceMultiplier);

                if (totalWeight <= 0)
                    throw new InvalidOperationException(
                        "Hệ số giá ghế không hợp lệ.");

                var seatPrices = seats.Select(seat => new
                {
                    Seat = seat,
                    Price = Math.Round(
                        ticketAmount *
                        (decimal)seat.PriceMultiplier / totalWeight,
                        2)
                }).ToList();

                // Điều chỉnh sai số làm tròn vào ghế cuối cùng
                decimal calculatedTotal = seatPrices.Sum(x => x.Price);
                decimal difference = ticketAmount - calculatedTotal;

                // 6. Tạo đơn đặt vé
                long bookingId = connection.ExecuteScalar<long>(@"
                    INSERT INTO Bookings (
                        UserId,
                        BookingCode,
                        BookingTime,
                        TotalAmount,
                        DiscountAmount,
                        FinalAmount,
                        PaymentMethod,
                        PaymentStatus,
                        BookingStatus,
                        PromotionCode
                    )
                    VALUES (
                        @UserId,
                        @BookingCode,
                        @BookingTime,
                        @TotalAmount,
                        @DiscountAmount,
                        @FinalAmount,
                        @PaymentMethod,
                        'PENDING',
                        'CONFIRMED',
                        @PromotionCode
                    );

                    SELECT last_insert_rowid();
                ",
                new
                {
                    UserId = userId,
                    BookingCode = booking.BookingCode,
                    BookingTime = booking.BookingTime == default
                        ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        : booking.BookingTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    TotalAmount = grossAmount,
                    DiscountAmount = booking.DiscountAmount,
                    FinalAmount = booking.TotalAmount,
                    PaymentMethod = booking.PaymentMethod,
                    PromotionCode = booking.PromotionCode
                }, transaction);

                // 7. Lưu từng vé
                for (int i = 0; i < seatPrices.Count; i++)
                {
                    var item = seatPrices[i];

                    decimal price = item.Price;

                    if (i == seatPrices.Count - 1)
                        price += difference;

                    connection.Execute(@"
                        INSERT INTO Tickets (
                            BookingId,
                            ShowtimeId,
                            SeatId,
                            TicketCode,
                            SeatNumber,
                            Price,
                            Status
                        )
                        VALUES (
                            @BookingId,
                            @ShowtimeId,
                            @SeatId,
                            @TicketCode,
                            @SeatNumber,
                            @Price,
                            'BOOKED'
                        );
                    ",
                    new
                    {
                        BookingId = bookingId,
                        ShowtimeId = showtime.Id,
                        SeatId = item.Seat.Id,
                        TicketCode = $"{booking.BookingCode}-{item.Seat.SeatNumber}",
                        SeatNumber = item.Seat.SeatNumber,
                        Price = price
                    }, transaction);
                }

                // 8. Lưu từng sản phẩm bắp nước
                foreach (var item in booking.FoodCart)
                {
                    int productId = item.Key;
                    string productName = item.Value.Name;
                    decimal unitPrice = item.Value.Price;
                    int quantity = item.Value.Qty;

                    if (productId <= 0 || quantity <= 0 || unitPrice < 0)
                        throw new InvalidOperationException(
                            $"Thông tin sản phẩm {productName} không hợp lệ.");

                    // Trừ tồn kho ngay trong transaction lưu đơn.
                    // Nếu không đủ hàng, toàn bộ đơn (kể cả vé) sẽ rollback.
                    int affectedRows = connection.Execute(@"
                        UPDATE Products
                        SET StockQuantity = StockQuantity - @Quantity
                        WHERE Id = @ProductId
                          AND IsActive = 1
                          AND StockQuantity >= @Quantity;
                    ",
                    new
                    {
                        ProductId = productId,
                        Quantity = quantity
                    }, transaction);

                    if (affectedRows != 1)
                    {
                        int? availableStock = connection.QueryFirstOrDefault<int?>(@"
                            SELECT StockQuantity
                            FROM Products
                            WHERE Id = @ProductId;
                        ", new { ProductId = productId }, transaction);

                        if (!availableStock.HasValue)
                        {
                            throw new InvalidOperationException(
                                $"Sản phẩm {productName} không tồn tại trong database.");
                        }

                        throw new InvalidOperationException(
                            $"Sản phẩm {productName} không đủ tồn kho. " +
                            $"Còn lại: {availableStock.Value}, cần: {quantity}.");
                    }

                    connection.Execute(@"
                        INSERT INTO BookingDetailProducts (
                            BookingId,
                            ProductId,
                            ProductName,
                            Quantity,
                            UnitPrice,
                            TotalPrice
                        )
                        VALUES (
                            @BookingId,
                            @ProductId,
                            @ProductName,
                            @Quantity,
                            @UnitPrice,
                            @TotalPrice
                        );
                    ",
                    new
                    {
                        BookingId = bookingId,
                        ProductId = productId,
                        ProductName = productName,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        TotalPrice = unitPrice * quantity
                    }, transaction);
                }

                // 9. Hoàn tất giao dịch
                transaction.Commit();

                booking.UserId = userId;
                return bookingId;
            }
            catch (SqliteException ex) when (
                ex.SqliteErrorCode == 19)
            {
                transaction.Rollback();

                throw new InvalidOperationException(
                    "Ghế đã được người khác đặt hoặc dữ liệu đơn hàng bị trùng. " +
                    "Vui lòng tải lại danh sách ghế.",
                    ex);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Kiểm tra dữ liệu trước khi lưu.
        /// </summary>
        private static void ValidateBooking(
            BookingResultEventArgs booking)
        {
            if (string.IsNullOrWhiteSpace(booking.BookingCode))
                throw new InvalidOperationException(
                    "Mã đặt vé không được để trống.");

            if (booking.Movie == null || booking.Movie.Id <= 0)
                throw new InvalidOperationException(
                    "Thông tin phim không hợp lệ.");

            if (booking.Showtime == null || booking.Showtime.Id <= 0)
                throw new InvalidOperationException(
                    "Thông tin suất chiếu không hợp lệ.");

            if (booking.Seats == null || booking.Seats.Count == 0)
                throw new InvalidOperationException(
                    "Bạn chưa chọn ghế.");

            if (booking.Seats.Count != booking.Seats
                .Distinct(StringComparer.OrdinalIgnoreCase).Count())
                throw new InvalidOperationException(
                    "Danh sách ghế có ghế bị trùng.");

            if (booking.TotalAmount < 0 ||
                booking.DiscountAmount < 0 ||
                booking.TicketAmount < 0 ||
                booking.FoodAmount < 0)
                throw new InvalidOperationException(
                    "Số tiền thanh toán không hợp lệ.");

            if (booking.FoodCart == null)
                throw new InvalidOperationException(
                    "Giỏ hàng bắp nước không hợp lệ.");

            foreach (var item in booking.FoodCart)
            {
                if (item.Key <= 0 ||
                    item.Value.Qty <= 0 ||
                    item.Value.Price < 0)
                {
                    throw new InvalidOperationException(
                        $"Thông tin sản phẩm {item.Value.Name} không hợp lệ.");
                }
            }
        }

        // =========================================================
        // TRUY VẤN ĐƠN ĐẶT VÉ TỪ SQLITE
        // =========================================================

        /// <summary>
        /// Lấy toàn bộ lịch sử đặt vé của một tài khoản (mới nhất lên đầu).
        /// </summary>
        public List<BookingResultEventArgs> GetBookingsByUserId(int userId)
        {
            if (userId <= 0)
                return new List<BookingResultEventArgs>();

            using var connection = DatabaseConnection.GetConnection();
            connection.Open();

            var rows = connection.Query<BookingRow>(@"
                SELECT
                    b.Id,
                    b.UserId,
                    b.BookingCode,
                    b.BookingTime,
                    b.TotalAmount,
                    b.DiscountAmount,
                    b.FinalAmount,
                    b.PaymentMethod,
                    b.PromotionCode,
                    COALESCE(u.FullName, '') AS CustomerName
                FROM Bookings b
                LEFT JOIN Users u ON u.Id = b.UserId
                WHERE b.UserId = @UserId
                ORDER BY b.Id DESC;
            ", new { UserId = userId }).ToList();

            return BuildBookingResults(connection, rows);
        }

        /// <summary>
        /// Lấy đơn đặt vé gần nhất của một tài khoản.
        /// </summary>
        public BookingResultEventArgs? GetLatestBookingByUserId(int userId)
        {
            if (userId <= 0)
                return null;

            using var connection = DatabaseConnection.GetConnection();
            connection.Open();

            var row = connection.QueryFirstOrDefault<BookingRow>(@"
                SELECT
                    b.Id,
                    b.UserId,
                    b.BookingCode,
                    b.BookingTime,
                    b.TotalAmount,
                    b.DiscountAmount,
                    b.FinalAmount,
                    b.PaymentMethod,
                    b.PromotionCode,
                    COALESCE(u.FullName, '') AS CustomerName
                FROM Bookings b
                LEFT JOIN Users u ON u.Id = b.UserId
                WHERE b.UserId = @UserId
                ORDER BY b.Id DESC
                LIMIT 1;
            ", new { UserId = userId });

            if (row == null)
                return null;

            return BuildBookingResults(connection, new List<BookingRow> { row })
                .FirstOrDefault();
        }

        /// <summary>
        /// Tìm đơn đặt vé theo mã BookingCode (dùng cho tra cứu / soát vé).
        /// </summary>
        public BookingResultEventArgs? GetBookingByCode(string bookingCode)
        {
            if (string.IsNullOrWhiteSpace(bookingCode))
                return null;

            using var connection = DatabaseConnection.GetConnection();
            connection.Open();

            var row = connection.QueryFirstOrDefault<BookingRow>(@"
                SELECT
                    b.Id,
                    b.UserId,
                    b.BookingCode,
                    b.BookingTime,
                    b.TotalAmount,
                    b.DiscountAmount,
                    b.FinalAmount,
                    b.PaymentMethod,
                    b.PromotionCode,
                    COALESCE(u.FullName, '') AS CustomerName
                FROM Bookings b
                LEFT JOIN Users u ON u.Id = b.UserId
                WHERE UPPER(b.BookingCode) = UPPER(@BookingCode)
                LIMIT 1;
            ", new { BookingCode = bookingCode.Trim() });

            if (row == null)
                return null;

            return BuildBookingResults(connection, new List<BookingRow> { row })
                .FirstOrDefault();
        }

        /// <summary>
        /// Lấy toàn bộ đơn có vé trong hệ thống (phục vụ màn hình soát vé).
        /// </summary>
        public List<BookingResultEventArgs> GetAllTicketBookings()
        {
            using var connection = DatabaseConnection.GetConnection();
            connection.Open();

            var rows = connection.Query<BookingRow>(@"
                SELECT
                    b.Id,
                    b.UserId,
                    b.BookingCode,
                    b.BookingTime,
                    b.TotalAmount,
                    b.DiscountAmount,
                    b.FinalAmount,
                    b.PaymentMethod,
                    b.PromotionCode,
                    COALESCE(u.FullName, '') AS CustomerName
                FROM Bookings b
                LEFT JOIN Users u ON u.Id = b.UserId
                WHERE EXISTS (
                    SELECT 1 FROM Tickets t WHERE t.BookingId = b.Id
                )
                ORDER BY b.Id DESC;
            ").ToList();

            return BuildBookingResults(connection, rows);
        }

        /// <summary>
        /// Kiểm tra đơn đặt vé trong SQLite đã được soát vé (USED) hay chưa.
        /// </summary>
        public bool IsBookingCheckedIn(string bookingCode)
        {
            if (string.IsNullOrWhiteSpace(bookingCode))
                return false;

            using var connection = DatabaseConnection.GetConnection();
            connection.Open();

            int usedCount = connection.ExecuteScalar<int>(@"
                SELECT COUNT(1)
                FROM Tickets t
                INNER JOIN Bookings b ON b.Id = t.BookingId
                WHERE UPPER(b.BookingCode) = UPPER(@BookingCode)
                  AND UPPER(t.Status) = 'USED';
            ", new { BookingCode = bookingCode.Trim() });

            return usedCount > 0;
        }

        /// <summary>
        /// Đánh dấu các vé thuộc đơn đã được soát (USED) trong SQLite.
        /// </summary>
        public bool MarkBookingCheckedIn(string bookingCode)
        {
            if (string.IsNullOrWhiteSpace(bookingCode))
                return false;

            using var connection = DatabaseConnection.GetConnection();
            connection.Open();

            int affected = connection.Execute(@"
                UPDATE Tickets
                SET Status = 'USED'
                WHERE BookingId = (
                    SELECT Id FROM Bookings
                    WHERE UPPER(BookingCode) = UPPER(@BookingCode)
                    LIMIT 1
                )
                AND UPPER(Status) IN ('BOOKED', 'CONFIRMED');
            ", new { BookingCode = bookingCode.Trim() });

            return affected > 0;
        }

        private static List<BookingResultEventArgs> BuildBookingResults(
            SqliteConnection connection,
            List<BookingRow> bookingRows)
        {
            if (bookingRows.Count == 0)
                return new List<BookingResultEventArgs>();

            var bookingIds = bookingRows.Select(b => b.Id).ToList();

            var ticketRows = connection.Query<TicketRow>(@"
                SELECT
                    BookingId,
                    ShowtimeId,
                    SeatNumber,
                    Price,
                    Status
                FROM Tickets
                WHERE BookingId IN @BookingIds
                ORDER BY Id;
            ", new { BookingIds = bookingIds }).ToList();

            var foodRows = connection.Query<FoodRow>(@"
                SELECT
                    d.BookingId,
                    COALESCE(d.ProductId, 0) AS ProductId,
                    d.ProductName,
                    d.Quantity,
                    d.UnitPrice,
                    d.TotalPrice,
                    COALESCE(p.ImageUrl, '') AS ImageUrl
                FROM BookingDetailProducts d
                LEFT JOIN Products p ON p.Id = d.ProductId
                WHERE d.BookingId IN @BookingIds
                ORDER BY d.Id;
            ", new { BookingIds = bookingIds }).ToList();

            var showtimeIds = ticketRows
                .Select(t => t.ShowtimeId)
                .Distinct()
                .ToList();

            var showtimeMap = showtimeIds.Count > 0
                ? connection.Query<Showtime>(@"
                    SELECT Id, MovieId, RoomId, StartTime, EndTime, BasePrice, Format
                    FROM Showtimes
                    WHERE Id IN @Ids;
                ", new { Ids = showtimeIds }).ToDictionary(s => s.Id)
                : new Dictionary<int, Showtime>();

            var movieIds = showtimeMap.Values
                .Select(s => s.MovieId)
                .Distinct()
                .ToList();

            var movieMap = movieIds.Count > 0
                ? connection.Query<Movie>(@"
                    SELECT Id, Title, Description, DurationMinutes, Director,
                           Genre, PosterUrl, ReleaseDate, Status
                    FROM Movies
                    WHERE Id IN @Ids;
                ", new { Ids = movieIds }).ToDictionary(m => m.Id)
                : new Dictionary<int, Movie>();

            var ticketsByBooking = ticketRows
                .GroupBy(t => t.BookingId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var foodsByBooking = foodRows
                .GroupBy(f => f.BookingId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var results = new List<BookingResultEventArgs>(bookingRows.Count);

            foreach (var row in bookingRows)
            {
                ticketsByBooking.TryGetValue(row.Id, out var tickets);
                tickets ??= new List<TicketRow>();

                foodsByBooking.TryGetValue(row.Id, out var foods);
                foods ??= new List<FoodRow>();

                Showtime? showtime = null;
                Movie? movie = null;

                if (tickets.Count > 0 &&
                    showtimeMap.TryGetValue(tickets[0].ShowtimeId, out showtime))
                {
                    movieMap.TryGetValue(showtime.MovieId, out movie);
                }

                var foodCart = new Dictionary<int, (
                    string Name,
                    decimal Price,
                    int Qty,
                    string ImgPath)>();

                int fallbackKey = -1;
                foreach (var f in foods)
                {
                    int key = f.ProductId > 0 ? f.ProductId : fallbackKey--;
                    if (foodCart.TryGetValue(key, out var existing))
                    {
                        foodCart[key] = (
                            existing.Name,
                            existing.Price,
                            existing.Qty + f.Quantity,
                            existing.ImgPath);
                    }
                    else
                    {
                        foodCart[key] = (
                            f.ProductName,
                            f.UnitPrice,
                            f.Quantity,
                            f.ImageUrl);
                    }
                }

                decimal ticketAmount = tickets.Sum(t => t.Price);
                decimal foodAmount = foods.Sum(f => f.TotalPrice);

                results.Add(new BookingResultEventArgs
                {
                    UserId = row.UserId,
                    BookingCode = row.BookingCode,
                    Movie = movie,
                    Showtime = showtime,
                    Seats = tickets.Select(t => t.SeatNumber).ToList(),
                    FoodCart = foodCart,
                    TicketAmount = ticketAmount,
                    FoodAmount = foodAmount,
                    DiscountAmount = row.DiscountAmount,
                    TotalAmount = row.FinalAmount,
                    PromotionCode = row.PromotionCode ?? string.Empty,
                    BookingTime = row.BookingTime,
                    PaymentMethod = row.PaymentMethod ?? string.Empty,
                    Channel = row.BookingCode.StartsWith("POS", StringComparison.OrdinalIgnoreCase)
                        ? "POS"
                        : "Online",
                    CustomerName = row.CustomerName ?? string.Empty
                });
            }

            return results;
        }

        private sealed class BookingRow
        {
            public long Id { get; set; }
            public int? UserId { get; set; }
            public string BookingCode { get; set; } = string.Empty;
            public DateTime BookingTime { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal DiscountAmount { get; set; }
            public decimal FinalAmount { get; set; }
            public string? PaymentMethod { get; set; }
            public string? PromotionCode { get; set; }
            public string? CustomerName { get; set; }
        }

        private sealed class TicketRow
        {
            public long BookingId { get; set; }
            public int ShowtimeId { get; set; }
            public string SeatNumber { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public string Status { get; set; } = string.Empty;
        }

        private sealed class FoodRow
        {
            public long BookingId { get; set; }
            public int ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal TotalPrice { get; set; }
            public string ImageUrl { get; set; } = string.Empty;
        }
    }
}