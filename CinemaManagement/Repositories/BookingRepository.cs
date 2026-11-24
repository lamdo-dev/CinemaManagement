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
    }
}