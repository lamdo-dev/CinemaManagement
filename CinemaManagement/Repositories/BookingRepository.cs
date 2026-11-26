using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using CinemaManagement.Data;
using CinemaManagement.Models;
using CinemaManagement.Models.Enums;
using CinemaManagement.Services;
using CinemaManagement.Utils;

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
            int? userId = null,
            int? staffUserId = null)
        {
            if (booking == null)
                throw new ArgumentNullException(nameof(booking));

            ValidateBooking(booking);

            using var connection = DatabaseConnection.GetConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                if (staffUserId.HasValue)
                    ValidateStaffUser(connection, transaction, staffUserId.Value);

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

                // Kiểm tra trạng thái bộ phim phải đang mở bán
                var movie = connection.QueryFirstOrDefault<Movie>(@"
                    SELECT Id, Title, Status
                    FROM Movies
                    WHERE Id = @MovieId;
                ", new { MovieId = showtime.MovieId }, transaction);

                if (movie == null)
                    throw new InvalidOperationException(
                        "Bộ phim của suất chiếu không còn tồn tại.");

                if (movie.Status == MovieStatus.Ended)
                    throw new InvalidOperationException(
                        "Bộ phim này đã kết thúc đợt chiếu, không thể đặt vé.");

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
                           RowIndex, ColumnIndex, Type,
                           CAST(PriceMultiplier AS REAL) AS PriceMultiplier
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

                // Kiểm tra ghế đã có người đặt trong cùng transaction (bỏ qua vé CANCELLED)
                var alreadyBookedSeats = connection.Query<string>(@"
                    SELECT SeatNumber
                    FROM Tickets
                    WHERE ShowtimeId = @ShowtimeId
                      AND SeatId IN @SeatIds
                      AND UPPER(Status) <> 'CANCELLED';
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

                // 4. Tính lại giá từng ghế trực tiếp từ dữ liệu DB (PricingRules / Showtime + Seat)
                var pricingRules = connection.Query<PricingRule>(@"
                    SELECT Id, Format, SeatType, DayType, TimeSlot, BasePrice, Surcharge
                    FROM PricingRules;
                ", transaction: transaction).ToList();

                var pricingService = new PricingService();
                var seatPrices = seats.Select(seat => new
                {
                    Seat = seat,
                    Price = pricingService.CalculateSeatPrice(showtime, seat, pricingRules)
                }).ToList();

                decimal dbTicketAmount = seatPrices.Sum(x => x.Price);

                // 5. Đối chiếu giá bắp nước trực tiếp từ bảng Products trong DB
                var verifiedFoodItems = new List<(int ProductId, string Name, decimal UnitPrice, int Quantity, decimal LineTotal)>();
                decimal dbFoodAmount = 0m;

                foreach (var item in booking.FoodCart)
                {
                    int productId = item.Key;
                    int quantity = item.Value.Qty;

                    if (productId <= 0 || quantity <= 0)
                        throw new InvalidOperationException(
                            $"Thông tin sản phẩm {item.Value.Name} không hợp lệ.");

                    var dbProduct = connection.QueryFirstOrDefault<Product>(@"
                        SELECT Id, Name, Category, Price, StockQuantity, ImageUrl, IsActive
                        FROM Products
                        WHERE Id = @ProductId;
                    ", new { ProductId = productId }, transaction);

                    if (dbProduct == null)
                        throw new InvalidOperationException(
                            $"Sản phẩm {item.Value.Name} không tồn tại trong cơ sở dữ liệu.");

                    if (!dbProduct.IsActive)
                        throw new InvalidOperationException(
                            $"Sản phẩm {dbProduct.Name} hiện đã ngừng kinh doanh.");

                    if (dbProduct.Price < 0)
                        throw new InvalidOperationException(
                            $"Giá bán của sản phẩm {dbProduct.Name} không hợp lệ.");

                    decimal lineTotal = dbProduct.Price * quantity;
                    dbFoodAmount += lineTotal;

                    verifiedFoodItems.Add((
                        dbProduct.Id,
                        dbProduct.Name,
                        dbProduct.Price,
                        quantity,
                        lineTotal));
                }

                // 6. Kiểm tra lại mã khuyến mãi, lượt dùng và giới hạn 1 lần/tài khoản trong transaction
                decimal grossAmount = dbTicketAmount + dbFoodAmount;
                decimal dbDiscountAmount = 0m;
                string normalizedPromoCode = (booking.PromotionCode ?? string.Empty)
                    .Trim()
                    .ToUpperInvariant();

                if (!string.IsNullOrEmpty(normalizedPromoCode))
                {
                    var dbPromotion = connection.QueryFirstOrDefault<Promotion>(@"
                        SELECT Id, Code, Name, DiscountType, DiscountValue,
                               MaxDiscount, MinimumOrder,
                               COALESCE(UsageLimit, 100) AS UsageLimit,
                               COALESCE(UsedCount, 0) AS UsedCount,
                               StartDate, EndDate, IsEnabled
                        FROM Promotions
                        WHERE UPPER(Code) = @Code;
                    ", new { Code = normalizedPromoCode }, transaction);

                    if (dbPromotion == null)
                        throw new InvalidOperationException(
                            $"Mã voucher '{normalizedPromoCode}' không tồn tại.");

                    (_, dbDiscountAmount) = PromotionService.CalculateDiscountForPromotion(
                        dbPromotion,
                        grossAmount);

                    normalizedPromoCode = dbPromotion.Code.Trim().ToUpperInvariant();

                    if (userId.HasValue && userId.Value > 0)
                    {
                        bool alreadyUsedByUser = connection.ExecuteScalar<bool>(@"
                            SELECT EXISTS(
                                SELECT 1
                                FROM Bookings
                                WHERE UserId = @UserId
                                  AND UPPER(PromotionCode) = @Code
                                  AND UPPER(BookingStatus) <> 'CANCELLED'
                            );
                        ",
                        new
                        {
                            UserId = userId.Value,
                            Code = normalizedPromoCode
                        }, transaction);

                        if (alreadyUsedByUser)
                        {
                            throw new InvalidOperationException(
                                $"Tài khoản của bạn đã sử dụng mã '{normalizedPromoCode}' trước đó (mỗi tài khoản chỉ được dùng 1 lần).");
                        }
                    }

                    // Tăng lượt đã dùng một cách nguyên tử ngay trong transaction
                    int promoAffected = connection.Execute(@"
                        UPDATE Promotions
                        SET UsedCount = COALESCE(UsedCount, 0) + 1
                        WHERE Id = @PromotionId
                          AND IsEnabled = 1
                          AND (UsageLimit <= 0 OR COALESCE(UsedCount, 0) < UsageLimit);
                    ", new { PromotionId = dbPromotion.Id }, transaction);

                    if (promoAffected != 1)
                    {
                        throw new InvalidOperationException(
                            "Voucher vừa hết lượt sử dụng hoặc đã bị tạm ngưng.");
                    }
                }
                else if (booking.DiscountAmount > 0m)
                {
                    throw new InvalidOperationException(
                        "Không thể áp dụng giảm giá khi không có mã voucher hợp lệ.");
                }

                decimal dbFinalAmount = grossAmount - dbDiscountAmount;

                if (dbFinalAmount <= 0m)
                {
                    throw new InvalidOperationException(
                        "Tổng tiền thanh toán sau giảm giá phải lớn hơn 0 đ.");
                }

                if (Math.Abs(dbFinalAmount - booking.TotalAmount) > 0.01m)
                {
                    throw new InvalidOperationException(
                        "Giá vé, sản phẩm bắp nước hoặc ưu đãi vừa có thay đổi trên hệ thống. " +
                        "Vui lòng kiểm tra lại thông tin thanh toán.");
                }

                // 7. Xác định trạng thái thanh toán thực tế
                bool isPayAtCounter =
                    (booking.Channel?.StartsWith("Tại quầy", StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (booking.PaymentMethod?.Contains("Tại Quầy", StringComparison.OrdinalIgnoreCase) ?? false);

                string paymentStatus = (isPayAtCounter && dbFinalAmount > 0m)
                    ? "PENDING"
                    : "PAID";

                string channel = string.IsNullOrWhiteSpace(booking.Channel)
                    ? "Online"
                    : booking.Channel.Trim();

                string customerName = (booking.CustomerName ?? string.Empty).Trim();

                // 8. Tạo đơn đặt vé
                long bookingId = connection.ExecuteScalar<long>(@"
                    INSERT INTO Bookings (
                        UserId,
                        StaffUserId,
                        BookingCode,
                        BookingTime,
                        TotalAmount,
                        DiscountAmount,
                        FinalAmount,
                        PaymentMethod,
                        PaymentStatus,
                        BookingStatus,
                        PromotionCode,
                        Channel,
                        CustomerName
                    )
                    VALUES (
                        @UserId,
                        @StaffUserId,
                        @BookingCode,
                        @BookingTime,
                        @TotalAmount,
                        @DiscountAmount,
                        @FinalAmount,
                        @PaymentMethod,
                        @PaymentStatus,
                        'CONFIRMED',
                        @PromotionCode,
                        @Channel,
                        @CustomerName
                    );

                    SELECT last_insert_rowid();
                ",
                new
                {
                    UserId = userId,
                    StaffUserId = staffUserId,
                    BookingCode = booking.BookingCode,
                    BookingTime = booking.BookingTime == default
                        ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        : booking.BookingTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    TotalAmount = grossAmount,
                    DiscountAmount = dbDiscountAmount,
                    FinalAmount = dbFinalAmount,
                    PaymentMethod = booking.PaymentMethod,
                    PaymentStatus = paymentStatus,
                    PromotionCode = normalizedPromoCode,
                    Channel = channel,
                    CustomerName = customerName
                }, transaction);

                // 9. Lưu từng vé với đúng giá thực tế của ghế đó
                foreach (var item in seatPrices)
                {
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
                        Price = item.Price
                    }, transaction);
                }

                // 10. Trừ tồn kho và lưu chi tiết bắp nước theo đơn giá từ DB
                foreach (var food in verifiedFoodItems)
                {
                    int affectedRows = connection.Execute(@"
                        UPDATE Products
                        SET StockQuantity = StockQuantity - @Quantity
                        WHERE Id = @ProductId
                          AND IsActive = 1
                          AND StockQuantity >= @Quantity;
                    ",
                    new
                    {
                        ProductId = food.ProductId,
                        Quantity = food.Quantity
                    }, transaction);

                    if (affectedRows != 1)
                    {
                        int availableStock = connection.QueryFirstOrDefault<int>(@"
                            SELECT COALESCE(StockQuantity, 0)
                            FROM Products
                            WHERE Id = @ProductId;
                        ", new { ProductId = food.ProductId }, transaction);

                        throw new InvalidOperationException(
                            $"Sản phẩm {food.Name} không đủ tồn kho. " +
                            $"Còn lại: {availableStock}, cần: {food.Quantity}.");
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
                        ProductId = food.ProductId,
                        ProductName = food.Name,
                        Quantity = food.Quantity,
                        UnitPrice = food.UnitPrice,
                        TotalPrice = food.LineTotal
                    }, transaction);
                }

                // 11. Cộng điểm thưởng thành viên (1 điểm cho mỗi 1.000 đ đã thanh toán)
                int earnedPoints = 0;
                if (userId.HasValue && paymentStatus == "PAID" && dbFinalAmount > 0m)
                {
                    earnedPoints = (int)Math.Floor(dbFinalAmount / 1000m);
                    if (earnedPoints > 0)
                    {
                        connection.Execute(@"
                            UPDATE Users
                            SET RewardPoints = COALESCE(RewardPoints, 0) + @EarnedPoints
                            WHERE Id = @UserId;
                        ",
                        new
                        {
                            UserId = userId.Value,
                            EarnedPoints = earnedPoints
                        }, transaction);
                    }
                }

                // 12. Hoàn tất giao dịch
                transaction.Commit();

                booking.UserId = userId;
                booking.TicketAmount = dbTicketAmount;
                booking.FoodAmount = dbFoodAmount;
                booking.DiscountAmount = dbDiscountAmount;
                booking.TotalAmount = dbFinalAmount;
                booking.PromotionCode = normalizedPromoCode;
                booking.PaymentStatus = paymentStatus;
                booking.Channel = channel;
                booking.CustomerName = customerName;
                booking.EarnedPoints = earnedPoints;

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
        /// Lưu hóa đơn chỉ gồm bắp nước/combo cho giao dịch POS.
        /// </summary>
        public long SaveFoodOnlyBooking(
            BookingResultEventArgs booking,
            int staffUserId)
        {
            if (booking == null)
                throw new ArgumentNullException(nameof(booking));

            if (booking.FoodCart == null || booking.FoodCart.Count == 0)
                throw new InvalidOperationException(
                    "Hóa đơn chưa có sản phẩm để lưu.");

            using var connection = DatabaseConnection.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                ValidateStaffUser(connection, transaction, staffUserId);

                var productIds = booking.FoodCart.Keys.ToList();
                var products = connection.Query<Product>(@"
                    SELECT Id, Name, Category, Price, StockQuantity, ImageUrl, IsActive
                    FROM Products
                    WHERE Id IN @ProductIds;
                ", new { ProductIds = productIds }, transaction)
                    .ToDictionary(product => product.Id);

                if (products.Count != productIds.Distinct().Count())
                    throw new InvalidOperationException(
                        "Một hoặc nhiều sản phẩm không còn trong cơ sở dữ liệu.");

                decimal foodAmount = 0m;
                foreach (var item in booking.FoodCart)
                {
                    if (item.Value.Qty <= 0)
                        throw new InvalidOperationException(
                            $"Số lượng sản phẩm {item.Value.Name} không hợp lệ.");

                    if (!products.TryGetValue(item.Key, out Product? product) || !product.IsActive)
                        throw new InvalidOperationException(
                            $"Sản phẩm {item.Value.Name} hiện không kinh doanh.");

                    foodAmount += product.Price * item.Value.Qty;
                }

                long bookingId = connection.ExecuteScalar<long>(@"
                    INSERT INTO Bookings (
                        UserId, StaffUserId, BookingCode, BookingTime, TotalAmount,
                        DiscountAmount, FinalAmount, PaymentMethod, PaymentStatus,
                        BookingStatus, PromotionCode, Channel, CustomerName)
                    VALUES (
                        NULL, @StaffUserId, @BookingCode, @BookingTime, @TotalAmount,
                        0, @TotalAmount, @PaymentMethod, 'PAID',
                        'CONFIRMED', '', 'POS', @CustomerName);
                    SELECT last_insert_rowid();
                ", new
                {
                    booking.BookingCode,
                    BookingTime = booking.BookingTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    TotalAmount = foodAmount,
                    StaffUserId = staffUserId,
                    booking.PaymentMethod,
                    CustomerName = booking.CustomerName ?? "Khách lẻ"
                }, transaction);

                foreach (var item in booking.FoodCart)
                {
                    Product product = products[item.Key];
                    decimal lineTotal = product.Price * item.Value.Qty;

                    int affectedRows = connection.Execute(@"
                        UPDATE Products
                        SET StockQuantity = StockQuantity - @Quantity
                        WHERE Id = @ProductId
                          AND IsActive = 1
                          AND StockQuantity >= @Quantity;
                    ", new
                    {
                        ProductId = product.Id,
                        Quantity = item.Value.Qty
                    }, transaction);

                    if (affectedRows != 1)
                        throw new InvalidOperationException(
                            $"Sản phẩm {product.Name} vừa hết hoặc không đủ tồn kho.");

                    connection.Execute(@"
                        INSERT INTO BookingDetailProducts (
                            BookingId, ProductId, ProductName, Quantity, UnitPrice, TotalPrice)
                        VALUES (
                            @BookingId, @ProductId, @ProductName, @Quantity, @UnitPrice, @TotalPrice);
                    ", new
                    {
                        BookingId = bookingId,
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Quantity = item.Value.Qty,
                        UnitPrice = product.Price,
                        TotalPrice = lineTotal
                    }, transaction);
                }

                transaction.Commit();

                booking.UserId = null;
                booking.TicketAmount = 0m;
                booking.FoodAmount = foodAmount;
                booking.DiscountAmount = 0m;
                booking.TotalAmount = foodAmount;
                booking.PaymentStatus = "PAID";
                booking.Channel = "POS";
                return bookingId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Lấy mã các đơn có ít nhất một vé đã được check-in.
        /// </summary>
        public HashSet<string> GetCheckedInBookingCodes()
        {
            using var connection = DatabaseConnection.GetConnection();
            connection.Open();

            return new HashSet<string>(connection.Query<string>(@"
                SELECT DISTINCT b.BookingCode
                FROM Bookings b
                INNER JOIN Tickets t ON t.BookingId = b.Id
                WHERE UPPER(t.Status) = 'USED'
                  AND UPPER(COALESCE(b.PaymentStatus, '')) = 'PAID'
                  AND UPPER(COALESCE(b.BookingStatus, '')) <> 'CANCELLED';
            "), StringComparer.OrdinalIgnoreCase);
        }

        public Dictionary<string, DateTime> GetBookingCheckInTimes()
        {
            using var connection = DatabaseConnection.GetConnection();
            connection.Open();

            var rows = connection.Query<BookingCheckInRow>(@"
                SELECT b.BookingCode, MAX(t.CheckedInAt) AS CheckedInAt
                FROM Bookings b
                INNER JOIN Tickets t ON t.BookingId = b.Id
                WHERE UPPER(t.Status) = 'USED'
                  AND t.CheckedInAt IS NOT NULL
                  AND UPPER(COALESCE(b.PaymentStatus, '')) = 'PAID'
                  AND UPPER(COALESCE(b.BookingStatus, '')) <> 'CANCELLED'
                GROUP BY b.BookingCode;
            ");

            var checkInTimes = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            foreach (BookingCheckInRow row in rows)
            {
                if (DateTime.TryParse(row.CheckedInAt, out DateTime checkInTime))
                    checkInTimes[row.BookingCode] = checkInTime;
            }

            return checkInTimes;
        }

        public DateTime? GetBookingCheckInTime(string bookingCode)
        {
            if (string.IsNullOrWhiteSpace(bookingCode))
                return null;

            using var connection = DatabaseConnection.GetConnection();
            connection.Open();

            string? checkedInAt = connection.QueryFirstOrDefault<string>(@"
                SELECT MAX(t.CheckedInAt)
                FROM Bookings b
                INNER JOIN Tickets t ON t.BookingId = b.Id
                WHERE UPPER(b.BookingCode) = UPPER(@BookingCode)
                  AND UPPER(t.Status) = 'USED';
            ", new { BookingCode = bookingCode.Trim() });

            return DateTime.TryParse(checkedInAt, out DateTime checkInTime)
                ? checkInTime
                : null;
        }

        public void SaveTicketScanLog(TicketScanResult result, int staffUserId)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            using var connection = DatabaseConnection.GetConnection();
            connection.Open();
            ValidateStaffUser(connection, null, staffUserId);

            connection.Execute(@"
                INSERT INTO StaffTicketScanLogs (
                    StaffUserId, BookingId, BookingCode, ScanStatus, Message, ScannedAt)
                VALUES (
                    @StaffUserId,
                    (SELECT Id FROM Bookings
                     WHERE UPPER(BookingCode) = UPPER(@BookingCode)
                     LIMIT 1),
                    @BookingCode, @ScanStatus, @Message, @ScannedAt);
            ", new
            {
                StaffUserId = staffUserId,
                BookingCode = result.Code,
                ScanStatus = result.Status.ToString(),
                result.Message,
                ScannedAt = result.ScanTime.ToString("yyyy-MM-dd HH:mm:ss.fff")
            });
        }

        public List<StaffTicketScanLog> GetRecentTicketScanLogs(int limit = 100)
        {
            using var connection = DatabaseConnection.GetConnection();
            connection.Open();

            return connection.Query<StaffTicketScanLog>(@"
                SELECT
                    l.Id,
                    l.BookingCode,
                    l.ScanStatus,
                    l.Message,
                    l.ScannedAt,
                    st.Id AS ShowtimeId,
                    COALESCE(NULLIF(b.CustomerName, ''), customer.FullName, 'Khách lẻ') AS CustomerName,
                    COALESCE(m.Title, '—') AS MovieTitle,
                    COALESCE(st.StartTime, '') AS ShowtimeStartTime,
                    COALESCE(GROUP_CONCAT(t.SeatNumber, ', '), '—') AS SeatNumbers,
                    COALESCE(staff.FullName, 'Không rõ') AS StaffName
                FROM StaffTicketScanLogs l
                LEFT JOIN Bookings b ON b.Id = l.BookingId
                LEFT JOIN Users customer ON customer.Id = b.UserId
                LEFT JOIN Users staff ON staff.Id = l.StaffUserId
                LEFT JOIN Tickets t ON t.BookingId = b.Id
                LEFT JOIN Showtimes st ON st.Id = t.ShowtimeId
                LEFT JOIN Movies m ON m.Id = st.MovieId
                GROUP BY l.Id
                ORDER BY l.Id DESC
                LIMIT @Limit;
            ", new { Limit = Math.Clamp(limit, 1, 1000) }).ToList();
        }

        public int GetRejectedTicketScanCount(
            int? showtimeId = null,
            int? movieId = null,
            DateTime? showDate = null)
        {
            using var connection = DatabaseConnection.GetConnection();
            connection.Open();

            return connection.ExecuteScalar<int>(@"
                SELECT COUNT(1)
                FROM StaffTicketScanLogs l
                WHERE UPPER(l.ScanStatus) <> 'VALID'
                  AND (
                      (@ShowtimeId IS NULL AND @MovieId IS NULL AND @ShowDate IS NULL)
                      OR EXISTS (
                      SELECT 1
                      FROM Tickets t
                      INNER JOIN Showtimes s ON s.Id = t.ShowtimeId
                      WHERE t.BookingId = l.BookingId
                        AND (@ShowtimeId IS NULL OR t.ShowtimeId = @ShowtimeId)
                        AND (@MovieId IS NULL OR s.MovieId = @MovieId)
                        AND (@ShowDate IS NULL OR date(s.StartTime) = date(@ShowDate))
                      )
                  );
            ", new
            {
                ShowtimeId = showtimeId,
                MovieId = movieId,
                ShowDate = showDate?.ToString("yyyy-MM-dd")
            });
        }

        /// <summary>
        /// Lấy tổng số vé hợp lệ và số vé đã check-in trực tiếp từ SQLite.
        /// </summary>
        public (int Total, int CheckedIn) GetTicketCheckInCounts(
            int? showtimeId = null,
            int? movieId = null,
            DateTime? showDate = null)
        {
            using var connection = DatabaseConnection.GetConnection();
            connection.Open();

            TicketCheckInCountRow row = connection.QuerySingle<TicketCheckInCountRow>(@"
                SELECT
                    COUNT(CASE WHEN UPPER(Status) <> 'CANCELLED' THEN 1 END) AS Total,
                    COUNT(CASE WHEN UPPER(Status) = 'USED' THEN 1 END) AS CheckedIn
                FROM Tickets t
                INNER JOIN Bookings b ON b.Id = t.BookingId
                INNER JOIN Showtimes s ON s.Id = t.ShowtimeId
                WHERE UPPER(COALESCE(b.PaymentStatus, '')) = 'PAID'
                  AND UPPER(COALESCE(b.BookingStatus, '')) <> 'CANCELLED'
                  AND (@ShowtimeId IS NULL OR t.ShowtimeId = @ShowtimeId)
                  AND (@MovieId IS NULL OR s.MovieId = @MovieId)
                  AND (@ShowDate IS NULL OR date(s.StartTime) = date(@ShowDate));
            ", new
            {
                ShowtimeId = showtimeId,
                MovieId = movieId,
                ShowDate = showDate?.ToString("yyyy-MM-dd")
            });
            return (row.Total, row.CheckedIn);
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

        private const string SelectBookingRowSql = @"
            SELECT
                b.Id,
                b.UserId,
                b.BookingCode,
                b.BookingTime,
                b.TotalAmount,
                b.DiscountAmount,
                b.FinalAmount,
                b.PaymentMethod,
                b.PaymentStatus,
                b.PromotionCode,
                COALESCE(b.Channel, '') AS Channel,
                COALESCE(NULLIF(b.CustomerName, ''), u.FullName, '') AS CustomerName
            FROM Bookings b
            LEFT JOIN Users u ON u.Id = b.UserId
        ";

        /// <summary>
        /// Lấy toàn bộ lịch sử đặt vé của một tài khoản (mới nhất lên đầu).
        /// </summary>
        public List<BookingResultEventArgs> GetBookingsByUserId(int userId)
        {
            if (userId <= 0)
                return new List<BookingResultEventArgs>();

            using var connection = DatabaseConnection.GetConnection();
            connection.Open();

            var rows = connection.Query<BookingRow>(
                SelectBookingRowSql + @"
                WHERE b.UserId = @UserId
                ORDER BY b.Id DESC;",
                new { UserId = userId }).ToList();

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

            var row = connection.QueryFirstOrDefault<BookingRow>(
                SelectBookingRowSql + @"
                WHERE b.UserId = @UserId
                ORDER BY b.Id DESC
                LIMIT 1;",
                new { UserId = userId });

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

            var row = connection.QueryFirstOrDefault<BookingRow>(
                SelectBookingRowSql + @"
                WHERE UPPER(b.BookingCode) = UPPER(@BookingCode)
                  AND UPPER(COALESCE(b.PaymentStatus, '')) = 'PAID'
                  AND UPPER(COALESCE(b.BookingStatus, '')) <> 'CANCELLED'
                  AND EXISTS (
                      SELECT 1 FROM Tickets t
                      WHERE t.BookingId = b.Id
                        AND UPPER(t.Status) <> 'CANCELLED'
                  )
                LIMIT 1;",
                new { BookingCode = bookingCode.Trim() });

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

            var rows = connection.Query<BookingRow>(
                SelectBookingRowSql + @"
                WHERE EXISTS (
                    SELECT 1 FROM Tickets t
                    WHERE t.BookingId = b.Id
                      AND UPPER(t.Status) <> 'CANCELLED'
                )
                  AND UPPER(COALESCE(b.PaymentStatus, '')) = 'PAID'
                  AND UPPER(COALESCE(b.BookingStatus, '')) <> 'CANCELLED'
                ORDER BY b.Id DESC;").ToList();

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
        public bool MarkBookingCheckedIn(
            string bookingCode,
            int staffUserId,
            DateTime scanTime)
        {
            if (string.IsNullOrWhiteSpace(bookingCode))
                return false;

            using var connection = DatabaseConnection.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                ValidateStaffUser(connection, transaction, staffUserId);

                int affected = connection.Execute(@"
                    UPDATE Tickets
                    SET Status = 'USED',
                        CheckedInAt = @CheckedInAt,
                        CheckedInByUserId = @StaffUserId
                    WHERE BookingId = (
                        SELECT Id FROM Bookings
                        WHERE UPPER(BookingCode) = UPPER(@BookingCode)
                          AND UPPER(COALESCE(PaymentStatus, '')) = 'PAID'
                          AND UPPER(COALESCE(BookingStatus, '')) <> 'CANCELLED'
                        LIMIT 1
                    )
                    AND UPPER(Status) IN ('BOOKED', 'CONFIRMED');
                ", new
                {
                    BookingCode = bookingCode.Trim(),
                    StaffUserId = staffUserId,
                    CheckedInAt = scanTime.ToString("yyyy-MM-dd HH:mm:ss.fff")
                }, transaction);

                if (affected > 0)
                {
                    connection.Execute(@"
                        INSERT INTO StaffTicketScanLogs (
                            StaffUserId, BookingId, BookingCode, ScanStatus, Message, ScannedAt)
                        SELECT @StaffUserId, Id, BookingCode, 'Valid',
                               'Vé hợp lệ. Mời khách vào phòng chiếu.', @ScannedAt
                        FROM Bookings
                        WHERE UPPER(BookingCode) = UPPER(@BookingCode)
                        LIMIT 1;
                    ", new
                    {
                        StaffUserId = staffUserId,
                        BookingCode = bookingCode.Trim(),
                        ScannedAt = scanTime.ToString("yyyy-MM-dd HH:mm:ss.fff")
                    }, transaction);
                }

                transaction.Commit();
                return affected > 0;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private static void ValidateStaffUser(
            SqliteConnection connection,
            SqliteTransaction? transaction,
            int staffUserId)
        {
            if (staffUserId <= 0)
                throw new InvalidOperationException("Tài khoản nhân viên không hợp lệ.");

            bool isStaff = connection.ExecuteScalar<bool>(@"
                SELECT EXISTS(
                    SELECT 1 FROM Users
                    WHERE Id = @StaffUserId
                      AND Role IN @AllowedRoles
                );
            ", new
            {
                StaffUserId = staffUserId,
                AllowedRoles = new[] { (int)UserRole.Admin, (int)UserRole.Staff }
            }, transaction);

            if (!isStaff)
                throw new InvalidOperationException(
                    "Chỉ tài khoản nhân viên hoặc quản trị viên mới được thực hiện thao tác này.");
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
                  AND UPPER(Status) <> 'CANCELLED'
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

                var foodCart = new Dictionary<int, CartItem>();

                int fallbackKey = -1;
                foreach (var f in foods)
                {
                    int key = f.ProductId > 0 ? f.ProductId : fallbackKey--;
                    if (foodCart.TryGetValue(key, out var existing))
                    {
                        foodCart[key] = new CartItem(
                            existing.Name,
                            existing.Price,
                            existing.Qty + f.Quantity,
                            existing.ImgPath);
                    }
                    else
                    {
                        foodCart[key] = new CartItem(
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
                    PaymentStatus = string.IsNullOrWhiteSpace(row.PaymentStatus)
                        ? "PAID"
                        : row.PaymentStatus,
                    Channel = !string.IsNullOrWhiteSpace(row.Channel)
                        ? row.Channel
                        : (row.BookingCode.StartsWith("POS", StringComparison.OrdinalIgnoreCase)
                            ? "POS"
                            : "Online"),
                    CustomerName = row.CustomerName ?? string.Empty,
                    EarnedPoints = string.Equals(row.PaymentStatus, "PAID", StringComparison.OrdinalIgnoreCase) &&
                                   row.UserId.HasValue &&
                                   row.FinalAmount > 0m
                        ? (int)Math.Floor(row.FinalAmount / 1000m)
                        : 0
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
            public string? PaymentStatus { get; set; }
            public string? PromotionCode { get; set; }
            public string? Channel { get; set; }
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

        private sealed class BookingCheckInRow
        {
            public string BookingCode { get; set; } = string.Empty;
            public string CheckedInAt { get; set; } = string.Empty;
        }

        private sealed class TicketCheckInCountRow
        {
            public int Total { get; set; }
            public int CheckedIn { get; set; }
        }
    }
}