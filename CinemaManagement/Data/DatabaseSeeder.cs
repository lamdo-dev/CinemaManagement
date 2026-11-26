using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CinemaManagement.Models;
using CinemaManagement.Models.Enums;
using CinemaManagement.Services;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CinemaManagement.Data;

public static class DatabaseSeeder
{
    /// <summary>
    /// Tự động nạp dữ liệu mẫu khi database chưa có dữ liệu nghiệp vụ (Movies trống).
    /// </summary>
    public static void SeedIfEmpty(SqliteConnection connection)
    {
        int movieCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Movies;");
        if (movieCount == 0)
        {
            SeedAll(connection, resetExisting: false);
        }
    }

    /// <summary>
    /// Khởi tạo đầy đủ dữ liệu mẫu cho toàn bộ các bảng để kiểm thử mọi phân hệ
    /// (Admin Dashboard, Quản lý Phim/Phòng/Ghế/Giá/Voucher/Bắp nước/User, Nhân viên POS & Soát vé, Khách hàng Đặt vé).
    /// </summary>
    public static void SeedAll(SqliteConnection connection, bool resetExisting = true)
    {
        connection.Execute("PRAGMA foreign_keys = OFF;");
        try
        {
            using var tx = connection.BeginTransaction();

            if (resetExisting)
            {
                connection.Execute(@"
                    DELETE FROM BookingDetailProducts;
                    DELETE FROM Tickets;
                    DELETE FROM Bookings;
                    DELETE FROM Showtimes;
                    DELETE FROM Seats;
                    DELETE FROM Rooms;
                    DELETE FROM PricingRules;
                    DELETE FROM Promotions;
                    DELETE FROM Products;
                    DELETE FROM Movies;
                    DELETE FROM Users;
                    DELETE FROM sqlite_sequence;
                ", transaction: tx);
            }

            DateTime now = DateTime.Now;
            DateTime today = now.Date;

            // =====================================================
            // 1. USERS (Admin, Staff, Customer)
            // =====================================================
            var users = new List<(string Username, string Password, string FullName, UserRole Role, string Email, string Phone, int BasePoints, DateTime CreatedAt)>
            {
                ("admin", "admin123", "Administrator", UserRole.Admin, "admin@cinema.vn", "0901000001", 0, today.AddDays(-90).AddHours(8)),
                ("quanly", "123456", "Nguyễn Minh Quân", UserRole.Admin, "quan.nm@cinema.vn", "0901000002", 500, today.AddDays(-75).AddHours(9)),
                ("staff", "staff123", "Trần Thu Hà", UserRole.Staff, "ha.tt@cinema.vn", "0902000001", 0, today.AddDays(-60).AddHours(8)),
                ("nhanvien01", "123456", "Lê Hoàng Nam", UserRole.Staff, "nam.lh@cinema.vn", "0902000002", 0, today.AddDays(-45).AddHours(8)),
                ("khachhang", "123456", "Phạm Quốc Bảo", UserRole.Customer, "bao.pq@gmail.com", "0912345678", 2400, today.AddDays(-60).AddHours(14)),
                ("nguyenvana", "123456", "Nguyễn Văn An", UserRole.Customer, "nguyenvana@gmail.com", "0987654321", 1200, today.AddDays(-40).AddHours(10)),
                ("tranthib", "123456", "Trần Thị Bích Ngọc", UserRole.Customer, "bichngoc.tran@gmail.com", "0933445566", 650, today.AddDays(-22).AddHours(16)),
                ("leminhc", "123456", "Lê Minh Châu", UserRole.Customer, "minhchau.le@gmail.com", "0977889900", 3100, today.AddDays(-14).AddHours(19)),
                ("hoangnam", "123456", "Hoàng Hải Nam", UserRole.Customer, "hainam.hoang@gmail.com", "0966112233", 200, today.AddDays(-4).AddHours(11)),
                ("vuthithao", "123456", "Vũ Thị Thảo", UserRole.Customer, "thaovu@gmail.com", "0944556677", 150, today.AddHours(9))
            };

            var userIdByUsername = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var fullNameByUserId = new Dictionary<int, string>();

            foreach (var u in users)
            {
                int id = connection.ExecuteScalar<int>(@"
                    INSERT INTO Users (Username, PasswordHash, FullName, Role, Email, Phone, RewardPoints, CreatedAt)
                    VALUES (@Username, @PasswordHash, @FullName, @Role, @Email, @Phone, @RewardPoints, @CreatedAt);
                    SELECT last_insert_rowid();
                ", new
                {
                    u.Username,
                    PasswordHash = u.Password,
                    u.FullName,
                    Role = (int)u.Role,
                    u.Email,
                    u.Phone,
                    RewardPoints = u.BasePoints,
                    CreatedAt = FmtDateTime(u.CreatedAt)
                }, tx);

                userIdByUsername[u.Username] = id;
                fullNameByUserId[id] = u.FullName;
            }

            // =====================================================
            // 2. MOVIES (Đang chiếu, Sắp chiếu, Đã kết thúc + Poster thật)
            // =====================================================
            var moviesSeed = new List<Movie>
            {
                new()
                {
                    Title = "Thám Tử Lừng Danh Conan: Dư Ảnh Của Độc Nhãn",
                    Genre = "Hành động, Trinh thám, Hoạt hình",
                    Director = "Katsuya Shigehara",
                    DurationMinutes = 110,
                    ReleaseDate = today.AddDays(-18),
                    Status = MovieStatus.Showing,
                    PosterUrl = @"Resources\Images\Movies\Conan_2025.jpg",
                    Description = "Conan cùng thanh tra Yamato Kansuke điều tra vụ án bí ẩn trên vùng núi tuyết Nagano, hé lộ ký ức bị đánh mất và âm mưu nguy hiểm đằng sau vết thương độc nhãn."
                },
                new()
                {
                    Title = "Doraemon Movie 45: Nobita Và Cuộc Phiêu Lưu Vào Thế Giới Trong Tranh",
                    Genre = "Hoạt hình, Phiêu lưu, Gia đình",
                    Director = "Yukiyo Teramoto",
                    DurationMinutes = 105,
                    ReleaseDate = today.AddDays(-12),
                    Status = MovieStatus.Showing,
                    PosterUrl = @"Resources\Images\Movies\Doraemon_movie45.jpg",
                    Description = "Nhóm bạn Nobita và Doraemon bước qua cánh cổng bảo bối tiến vào vương quốc hội họa thời Trung cổ châu Âu để giải cứu viên ngọc sắc màu huyền thoại."
                },
                new()
                {
                    Title = "Godzilla Minus One: Godzilla Trừ Không",
                    Genre = "Hành động, Viễn tưởng, Kịch tính",
                    Director = "Takashi Yamazaki",
                    DurationMinutes = 125,
                    ReleaseDate = today.AddDays(-25),
                    Status = MovieStatus.Showing,
                    PosterUrl = @"Resources\Images\Movies\Godzilla_Trừ_Không.jpg",
                    Description = "Nhật Bản thời hậu chiến đứng trước bờ vực sụp đổ hoàn toàn khi siêu quái thú khổng lồ Godzilla trỗi dậy từ đáy đại dương với sức hủy diệt kinh hoàng."
                },
                new()
                {
                    Title = "Tử Chiến Trên Không",
                    Genre = "Hành động, Giật gân",
                    Director = "Hàm Trần",
                    DurationMinutes = 118,
                    ReleaseDate = today.AddDays(-9),
                    Status = MovieStatus.Showing,
                    PosterUrl = @"Resources\Images\Movies\Tu_chien_tren_khong.jpg",
                    Description = "Chuyến bay định mệnh ở độ cao 10.000 mét biến thành cuộc đấu trí nghẹt thở từng giây giữa lực lượng an ninh hàng không và nhóm không tặc táo tợn."
                },
                new()
                {
                    Title = "Tom & Jerry: Quậy Tung New York",
                    Genre = "Hoạt hình, Hài hước, Gia đình",
                    Director = "Tim Story",
                    DurationMinutes = 101,
                    ReleaseDate = today.AddDays(-30),
                    Status = MovieStatus.Showing,
                    PosterUrl = @"Resources\Images\Movies\Tom_And_Jerry.jpg",
                    Description = "Cặp đôi mèo chuột huyền thoại Tom và Jerry gây náo loạn khách sạn sang trọng bậc nhất New York ngay trước thềm đám cưới thế kỷ."
                },
                new()
                {
                    Title = "Thám Tử Lừng Danh Conan Movie 29: Ngôi Sao Bí Ẩn",
                    Genre = "Trinh thám, Hoạt hình, Hành động",
                    Director = "Yuzuru Tachikawa",
                    DurationMinutes = 115,
                    ReleaseDate = today.AddDays(5),
                    Status = MovieStatus.ComingSoon,
                    PosterUrl = @"Resources\Images\Movies\Poster_Conan_Movie_29.jpg",
                    Description = "Siêu đạo chích Kaito Kid gửi mật thư thách thức toàn bộ sở cảnh sát Tokyo xoay quanh báu vật thiên văn cổ đại. Mở bán vé sớm suất chiếu đặc biệt!"
                },
                new()
                {
                    Title = "Chiến Binh Ngân Hà: Kỷ Nguyên Mới",
                    Genre = "Khoa học viễn tưởng, Hành động",
                    Director = "Denis Villeneuve",
                    DurationMinutes = 135,
                    ReleaseDate = today.AddDays(10),
                    Status = MovieStatus.ComingSoon,
                    PosterUrl = @"Resources\Images\Movies\Godzilla_Trừ_Không.jpg",
                    Description = "Hành trình vượt qua hố đen vũ trụ của hạm đội tiên phong nhân loại nhằm tìm kiếm hành tinh sự sống mới trước thảm họa diệt vong."
                },
                new()
                {
                    Title = "Doraemon: Nobita Và Đảo Giấu Vàng",
                    Genre = "Hoạt hình, Phiêu lưu",
                    Director = "Kazuaki Imai",
                    DurationMinutes = 109,
                    ReleaseDate = today.AddDays(-90),
                    Status = MovieStatus.Ended,
                    PosterUrl = @"Resources\Images\Movies\2018-doraemon-movie-poster.jpg",
                    Description = "Chuyến hải trình vượt Thái Bình Dương trên con tàu Vinh Quang của Nobita để khám phá bí mật hòn đảo kho báu."
                }
            };

            foreach (var m in moviesSeed)
            {
                m.Id = connection.ExecuteScalar<int>(@"
                    INSERT INTO Movies (Title, Description, DurationMinutes, Director, Genre, PosterUrl, ReleaseDate, Status)
                    VALUES (@Title, @Description, @DurationMinutes, @Director, @Genre, @PosterUrl, @ReleaseDate, @Status);
                    SELECT last_insert_rowid();
                ", new
                {
                    m.Title,
                    m.Description,
                    m.DurationMinutes,
                    m.Director,
                    m.Genre,
                    m.PosterUrl,
                    ReleaseDate = FmtDateTime(m.ReleaseDate),
                    Status = (int)m.Status
                }, tx);
            }

            // =====================================================
            // 3. ROOMS & SEATS (4 phòng đa dạng sức chứa & loại ghế)
            // =====================================================
            var roomDefs = new List<(string Name, int Rows, int Cols, int StandardRows, int VipRows)>
            {
                ("Phòng 01 - Standard 2D", 7, 10, 3, 3),    // 70 ghế (50-80): A-C Thường, D-F VIP, G Đôi
                ("Phòng 02 - IMAX 3D", 9, 12, 3, 5),        // 108 ghế (>80): A-C Thường, D-H VIP, I Đôi
                ("Phòng 03 - Gold Class VIP", 5, 8, 1, 3),  // 40 ghế (<50): A Thường, B-D VIP, E Đôi
                ("Phòng 04 - Dolby Atmos", 8, 10, 3, 4)     // 80 ghế (50-80): A-C Thường, D-G VIP, H Đôi
            };

            var rooms = new List<Room>();
            var seatsByRoom = new Dictionary<long, List<Seat>>();

            foreach (var def in roomDefs)
            {
                long roomId = connection.ExecuteScalar<long>(@"
                    INSERT INTO Rooms (Name, TotalSeats, TotalRows, TotalColumns)
                    VALUES (@Name, @TotalSeats, @TotalRows, @TotalColumns);
                    SELECT last_insert_rowid();
                ", new
                {
                    def.Name,
                    TotalSeats = def.Rows * def.Cols,
                    TotalRows = def.Rows,
                    TotalColumns = def.Cols
                }, tx);

                var room = new Room
                {
                    Id = roomId,
                    Name = def.Name,
                    TotalRows = def.Rows,
                    TotalColumns = def.Cols,
                    TotalSeats = def.Rows * def.Cols
                };
                rooms.Add(room);

                var roomSeatList = new List<Seat>();
                for (int r = 0; r < def.Rows; r++)
                {
                    SeatType seatType = r < def.StandardRows
                        ? SeatType.Standard
                        : (r < def.StandardRows + def.VipRows ? SeatType.VIP : SeatType.Couple);

                    double multiplier = seatType switch
                    {
                        SeatType.VIP => 1.2,
                        SeatType.Couple => 2.0,
                        _ => 1.0
                    };

                    for (int c = 0; c < def.Cols; c++)
                    {
                        string seatNumber = $"{(char)('A' + r)}{c + 1}";
                        int seatId = connection.ExecuteScalar<int>(@"
                            INSERT INTO Seats (RoomId, SeatNumber, RowIndex, ColumnIndex, Type, PriceMultiplier)
                            VALUES (@RoomId, @SeatNumber, @RowIndex, @ColumnIndex, @Type, @PriceMultiplier);
                            SELECT last_insert_rowid();
                        ", new
                        {
                            RoomId = roomId,
                            SeatNumber = seatNumber,
                            RowIndex = r,
                            ColumnIndex = c,
                            Type = (int)seatType,
                            PriceMultiplier = multiplier
                        }, tx);

                        roomSeatList.Add(new Seat
                        {
                            Id = seatId,
                            RoomId = (int)roomId,
                            SeatNumber = seatNumber,
                            RowIndex = r,
                            ColumnIndex = c,
                            Type = seatType,
                            PriceMultiplier = multiplier
                        });
                    }
                }

                seatsByRoom[roomId] = roomSeatList;
            }

            // =====================================================
            // 4. PRODUCTS (Đồ ăn, Đồ uống, Combo + sắp hết hàng + ngừng KD)
            // =====================================================
            var productsSeed = new List<Product>
            {
                new() { Name = "Bắp Rang Bơ Truyền Thống (L)", Category = "Đồ ăn", Price = 55000m, StockQuantity = 140, ImageUrl = @"Resources\Images\Products\Bắp_Rang_Bơ.jpg", IsActive = true },
                new() { Name = "Bắp Rang Phô Mai Hoàng Kim (L)", Category = "Đồ ăn", Price = 65000m, StockQuantity = 95, ImageUrl = @"Resources\Images\Products\Bắp_Rang_Bơ.jpg", IsActive = true },
                new() { Name = "Bắp Rang Caramel Ngọt Ngào (L)", Category = "Đồ ăn", Price = 65000m, StockQuantity = 8, ImageUrl = @"Resources\Images\Products\Bắp_Rang_Bơ.jpg", IsActive = true },
                new() { Name = "Coca-Cola Tươi Mát (L)", Category = "Đồ uống", Price = 35000m, StockQuantity = 180, ImageUrl = @"Resources\Images\Products\CoCaCoLa.jpg", IsActive = true },
                new() { Name = "Pepsi Không Đường (L)", Category = "Đồ uống", Price = 35000m, StockQuantity = 160, ImageUrl = @"Resources\Images\Products\PEPSI.jpg", IsActive = true },
                new() { Name = "Trà Đào Cam Sả Đặc Biệt", Category = "Đồ uống", Price = 45000m, StockQuantity = 6, ImageUrl = @"Resources\Images\Products\CoCaCoLa.jpg", IsActive = true },
                new() { Name = "Combo Solo (1 Bắp Bơ L + 1 Nước L)", Category = "Combo", Price = 80000m, StockQuantity = 120, ImageUrl = @"Resources\Images\Products\compo_bắp_nước.jpg", IsActive = true },
                new() { Name = "Combo Couple (1 Bắp Phô Mai L + 2 Nước L)", Category = "Combo", Price = 115000m, StockQuantity = 110, ImageUrl = @"Resources\Images\Products\compo_bắp_nước.jpg", IsActive = true },
                new() { Name = "Combo Family (2 Bắp L + 4 Nước L)", Category = "Combo", Price = 199000m, StockQuantity = 75, ImageUrl = @"Resources\Images\Products\compo_bắp_nước.jpg", IsActive = true },
                new() { Name = "Combo Lễ Hội Phiên Bản Giới Hạn", Category = "Combo", Price = 149000m, StockQuantity = 0, ImageUrl = @"Resources\Images\Products\compo_bắp_nước.jpg", IsActive = false }
            };

            foreach (var p in productsSeed)
            {
                p.Id = connection.ExecuteScalar<int>(@"
                    INSERT INTO Products (Name, Category, Price, StockQuantity, ImageUrl, IsActive)
                    VALUES (@Name, @Category, @Price, @StockQuantity, @ImageUrl, @IsActive);
                    SELECT last_insert_rowid();
                ", new
                {
                    p.Name,
                    p.Category,
                    p.Price,
                    p.StockQuantity,
                    p.ImageUrl,
                    IsActive = p.IsActive ? 1 : 0
                }, tx);
            }

            // =====================================================
            // 5. PRICING RULES (Đầy đủ 36 quy tắc 2D/3D - Thường/VIP/Đôi)
            // =====================================================
            var pricingRules = new List<PricingRule>();
            string[] formats = { "2D", "3D" };
            string[] seatTypes = { "Ghế thường", "Ghế VIP", "Ghế đôi" };
            string[] dayTypes = { "Ngày thường", "Cuối tuần" };
            string[] timeSlots = { "Trước 12:00", "12:00 - 17:00", "Sau 17:00" };

            foreach (string fmt in formats)
            {
                foreach (string st in seatTypes)
                {
                    decimal basePrice = (fmt, st) switch
                    {
                        ("2D", "Ghế thường") => 75000m,
                        ("2D", "Ghế VIP") => 95000m,
                        ("2D", "Ghế đôi") => 170000m,
                        ("3D", "Ghế thường") => 100000m,
                        ("3D", "Ghế VIP") => 125000m,
                        ("3D", "Ghế đôi") => 220000m,
                        _ => 80000m
                    };

                    foreach (string dt in dayTypes)
                    {
                        foreach (string ts in timeSlots)
                        {
                            decimal surcharge = (dt, ts) switch
                            {
                                ("Ngày thường", "Trước 12:00") => -10000m,
                                ("Ngày thường", "12:00 - 17:00") => 0m,
                                ("Ngày thường", "Sau 17:00") => 10000m,
                                ("Cuối tuần", "Trước 12:00") => 5000m,
                                ("Cuối tuần", "12:00 - 17:00") => 15000m,
                                ("Cuối tuần", "Sau 17:00") => 25000m,
                                _ => 0m
                            };

                            int ruleId = connection.ExecuteScalar<int>(@"
                                INSERT INTO PricingRules (Format, SeatType, DayType, TimeSlot, BasePrice, Surcharge)
                                VALUES (@Format, @SeatType, @DayType, @TimeSlot, @BasePrice, @Surcharge);
                                SELECT last_insert_rowid();
                            ", new
                            {
                                Format = fmt,
                                SeatType = st,
                                DayType = dt,
                                TimeSlot = ts,
                                BasePrice = basePrice,
                                Surcharge = surcharge
                            }, tx);

                            pricingRules.Add(new PricingRule
                            {
                                Id = ruleId,
                                Format = fmt,
                                SeatType = st,
                                DayType = dt,
                                TimeSlot = ts,
                                BasePrice = basePrice,
                                Surcharge = surcharge
                            });
                        }
                    }
                }
            }

            // =====================================================
            // 6. PROMOTIONS (Đang hoạt động, Sắp diễn ra, Đã kết thúc)
            // =====================================================
            var promotionsSeed = new List<Promotion>
            {
                new()
                {
                    Code = "HELLO2026",
                    Name = "Chào bạn mới - Giảm 20% tối đa 50K",
                    DiscountType = "Phần trăm",
                    DiscountValue = 20m,
                    MaxDiscount = 50000m,
                    MinimumOrder = 100000m,
                    UsageLimit = 200,
                    UsedCount = 0,
                    StartDate = today.AddDays(-30),
                    EndDate = today.AddDays(60),
                    IsEnabled = true
                },
                new()
                {
                    Code = "CINEMA30K",
                    Name = "Ưu đãi điện ảnh - Giảm ngay 30.000đ",
                    DiscountType = "Số tiền",
                    DiscountValue = 30000m,
                    MaxDiscount = 0m,
                    MinimumOrder = 150000m,
                    UsageLimit = 150,
                    UsedCount = 0,
                    StartDate = today.AddDays(-20),
                    EndDate = today.AddDays(45),
                    IsEnabled = true
                },
                new()
                {
                    Code = "COUPLE50K",
                    Name = "Hẹn hò cuối tuần - Giảm 50.000đ cho đơn từ 250K",
                    DiscountType = "Số tiền",
                    DiscountValue = 50000m,
                    MaxDiscount = 0m,
                    MinimumOrder = 250000m,
                    UsageLimit = 100,
                    UsedCount = 0,
                    StartDate = today.AddDays(-15),
                    EndDate = today.AddDays(30),
                    IsEnabled = true
                },
                new()
                {
                    Code = "VIPMEMBER",
                    Name = "Đặc quyền thành viên - Giảm 15% tối đa 80K",
                    DiscountType = "Phần trăm",
                    DiscountValue = 15m,
                    MaxDiscount = 80000m,
                    MinimumOrder = 180000m,
                    UsageLimit = 300,
                    UsedCount = 0,
                    StartDate = today.AddDays(-35),
                    EndDate = today.AddDays(90),
                    IsEnabled = true
                },
                new()
                {
                    Code = "BLOCKBUSTER25",
                    Name = "Tuần lễ bom tấn - Giảm 25% tối đa 70K (Sắp mở)",
                    DiscountType = "Phần trăm",
                    DiscountValue = 25m,
                    MaxDiscount = 70000m,
                    MinimumOrder = 200000m,
                    UsageLimit = 100,
                    UsedCount = 0,
                    StartDate = today.AddDays(5),
                    EndDate = today.AddDays(35),
                    IsEnabled = true
                },
                new()
                {
                    Code = "OLD10K",
                    Name = "Khuyến mãi tháng trước (Đã kết thúc)",
                    DiscountType = "Số tiền",
                    DiscountValue = 10000m,
                    MaxDiscount = 0m,
                    MinimumOrder = 90000m,
                    UsageLimit = 50,
                    UsedCount = 50,
                    StartDate = today.AddDays(-60),
                    EndDate = today.AddDays(-10),
                    IsEnabled = false
                }
            };

            var promoByCode = new Dictionary<string, Promotion>(StringComparer.OrdinalIgnoreCase);
            foreach (var pr in promotionsSeed)
            {
                pr.Id = connection.ExecuteScalar<int>(@"
                    INSERT INTO Promotions (
                        Code, Name, DiscountType, DiscountValue, MaxDiscount,
                        MinimumOrder, UsageLimit, UsedCount, StartDate, EndDate, IsEnabled
                    )
                    VALUES (
                        @Code, @Name, @DiscountType, @DiscountValue, @MaxDiscount,
                        @MinimumOrder, @UsageLimit, @UsedCount, @StartDate, @EndDate, @IsEnabled
                    );
                    SELECT last_insert_rowid();
                ", new
                {
                    pr.Code,
                    pr.Name,
                    pr.DiscountType,
                    pr.DiscountValue,
                    pr.MaxDiscount,
                    pr.MinimumOrder,
                    pr.UsageLimit,
                    pr.UsedCount,
                    StartDate = pr.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    EndDate = pr.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    IsEnabled = pr.IsEnabled ? 1 : 0
                }, tx);

                promoByCode[pr.Code] = pr;
            }

            // =====================================================
            // 7. SHOWTIMES (Không trùng phòng, phủ quá khứ + hôm nay + 6 ngày tới)
            // =====================================================
            var allShowtimes = new List<Showtime>();
            var pastShowtimesByOffset = new Dictionary<int, List<Showtime>>();
            var futureShowtimesByOffset = new Dictionary<int, List<Showtime>>();

            Showtime InsertShowtime(Movie movie, Room room, DateTime startTime, string format, decimal basePrice)
            {
                DateTime endTime = startTime.AddMinutes(movie.DurationMinutes + 10);
                int id = connection.ExecuteScalar<int>(@"
                    INSERT INTO Showtimes (MovieId, RoomId, StartTime, EndTime, BasePrice, Format)
                    VALUES (@MovieId, @RoomId, @StartTime, @EndTime, @BasePrice, @Format);
                    SELECT last_insert_rowid();
                ", new
                {
                    MovieId = movie.Id,
                    RoomId = (int)room.Id,
                    StartTime = FmtDateTime(startTime),
                    EndTime = FmtDateTime(endTime),
                    BasePrice = basePrice,
                    Format = format
                }, tx);

                var st = new Showtime
                {
                    Id = id,
                    MovieId = movie.Id,
                    RoomId = (int)room.Id,
                    StartTime = startTime,
                    EndTime = endTime,
                    BasePrice = basePrice,
                    Format = format
                };
                allShowtimes.Add(st);
                return st;
            }

            // 7.1 Lịch chiếu quá khứ (-45 ngày đến -1 ngày) phục vụ biểu đồ Dashboard Kỳ trước vs Kỳ này
            TimeSpan[] pastSlots =
            {
                new(9, 30, 0),
                new(13, 30, 0),
                new(17, 15, 0),
                new(20, 0, 0)
            };

            for (int dayOffset = -45; dayOffset <= -1; dayOffset++)
            {
                DateTime date = today.AddDays(dayOffset);
                var dayList = new List<Showtime>();

                // Tạo 2-3 suất mỗi ngày ở các phòng khác nhau để tỷ lệ lấp đầy ghế thực tế (45%-70%)
                int slotsCount = (dayOffset >= -14) ? 3 : 2;
                for (int s = 0; s < slotsCount; s++)
                {
                    int roomIdx = (Math.Abs(dayOffset) + s) % rooms.Count;
                    Room room = rooms[roomIdx];
                    int movieIdx = (Math.Abs(dayOffset * 2) + s) % 5; // 5 phim đang chiếu
                    Movie movie = moviesSeed[movieIdx];
                    TimeSpan slotTime = pastSlots[(Math.Abs(dayOffset) + s) % pastSlots.Length];
                    string format = (roomIdx == 1 || (roomIdx == 3 && s % 2 == 1)) ? "3D" : "2D";
                    decimal basePrice = format == "3D" ? 110000m : 85000m;

                    dayList.Add(InsertShowtime(movie, room, date.Add(slotTime), format, basePrice));
                }

                pastShowtimesByOffset[dayOffset] = dayList;
            }

            // 7.2 Lịch chiếu Hôm nay (dayOffset = 0) và 6 ngày tiếp theo (dayOffset = 1..6)
            // Khung giờ cách nhau >= 2h40p nên không bao giờ xung đột trong cùng phòng
            TimeSpan[] dailySlots =
            {
                new(8, 45, 0),
                new(11, 30, 0),
                new(14, 30, 0),
                new(17, 15, 0),
                new(19, 50, 0),
                new(22, 20, 0)
            };

            for (int dayOffset = 0; dayOffset <= 6; dayOffset++)
            {
                DateTime date = today.AddDays(dayOffset);
                var dayList = new List<Showtime>();

                for (int roomIdx = 0; roomIdx < rooms.Count; roomIdx++)
                {
                    Room room = rooms[roomIdx];
                    for (int slotIdx = 0; slotIdx < dailySlots.Length; slotIdx++)
                    {
                        Movie movie;
                        // Mở bán sớm 2 phim Sắp chiếu ở một số khung giờ tối
                        if (dayOffset >= 1 && roomIdx == 1 && slotIdx == 4)
                        {
                            movie = moviesSeed[5]; // Conan Movie 29 (ComingSoon)
                        }
                        else if (dayOffset >= 2 && roomIdx == 3 && slotIdx == 4)
                        {
                            movie = moviesSeed[6]; // Chiến Binh Ngân Hà (ComingSoon)
                        }
                        else
                        {
                            int movieIdx = (dayOffset + roomIdx * 2 + slotIdx) % 5;
                            movie = moviesSeed[movieIdx];
                        }

                        string format = roomIdx switch
                        {
                            1 => "3D", // IMAX 3D
                            3 => slotIdx >= 3 ? "3D" : "2D", // Dolby Atmos tối chiếu 3D
                            _ => "2D"
                        };

                        decimal basePrice = format == "3D" ? 115000m : 85000m;
                        DateTime startTime = date.Add(dailySlots[slotIdx]);
                        dayList.Add(InsertShowtime(movie, room, startTime, format, basePrice));
                    }
                }

                // Thêm 1 suất khuya 23:35 tại Phòng 03 (suất trước kết thúc ~21:45) để test đặt vé kể cả khuya muộn
                if (dayOffset == 0)
                {
                    dayList.Add(InsertShowtime(
                        moviesSeed[0],
                        rooms[2],
                        date.Add(new TimeSpan(23, 35, 0)),
                        "2D",
                        90000m));
                }

                futureShowtimesByOffset[dayOffset] = dayList;
            }

            // =====================================================
            // 8. BOOKINGS, TICKETS & FOOD DETAILS
            // =====================================================
            var pricingService = new PricingService();
            var usedSeatsPerShowtime = new Dictionary<int, HashSet<int>>();
            int bookingSeq = 1000;

            long InsertSeededBooking(
                string bookingCode,
                int? userId,
                string customerName,
                Showtime showtime,
                int seatCount,
                int preferredRowStart,
                List<(Product Product, int Qty)>? foodItems,
                string paymentMethod,
                string channel,
                DateTime bookingTime,
                string ticketStatus = "BOOKED",
                string bookingStatus = "CONFIRMED",
                string paymentStatus = "PAID",
                string? promoCode = null)
            {
                if (!usedSeatsPerShowtime.TryGetValue(showtime.Id, out var occupiedIds))
                {
                    occupiedIds = new HashSet<int>();
                    usedSeatsPerShowtime[showtime.Id] = occupiedIds;
                }

                var roomSeats = seatsByRoom[showtime.RoomId];
                var pickedSeats = roomSeats
                    .OrderBy(s => Math.Abs(s.RowIndex - preferredRowStart))
                    .ThenBy(s => Math.Abs(s.ColumnIndex - 4))
                    .Where(s => bookingStatus == "CANCELLED" || !occupiedIds.Contains(s.Id))
                    .Take(seatCount)
                    .OrderBy(s => s.RowIndex)
                    .ThenBy(s => s.ColumnIndex)
                    .ToList();

                if (pickedSeats.Count == 0)
                    return 0;

                decimal ticketTotal = 0m;
                var seatPrices = new List<(Seat Seat, decimal Price)>();
                foreach (var seat in pickedSeats)
                {
                    decimal price = pricingService.CalculateSeatPrice(showtime, seat, pricingRules);
                    ticketTotal += price;
                    seatPrices.Add((seat, price));
                    if (bookingStatus != "CANCELLED" && ticketStatus != "CANCELLED")
                    {
                        occupiedIds.Add(seat.Id);
                    }
                }

                decimal foodTotal = 0m;
                if (foodItems != null)
                {
                    foreach (var fi in foodItems)
                    {
                        foodTotal += fi.Product.Price * fi.Qty;
                    }
                }

                decimal grossAmount = ticketTotal + foodTotal;
                decimal discountAmount = 0m;
                string appliedPromo = string.Empty;

                if (!string.IsNullOrWhiteSpace(promoCode) &&
                    promoByCode.TryGetValue(promoCode, out var promo))
                {
                    if (promo.DiscountType == "Phần trăm")
                    {
                        discountAmount = Math.Round(grossAmount * promo.DiscountValue / 100m, 0);
                        if (promo.MaxDiscount > 0 && discountAmount > promo.MaxDiscount)
                            discountAmount = promo.MaxDiscount;
                    }
                    else
                    {
                        discountAmount = promo.DiscountValue;
                    }

                    if (discountAmount > 0 && discountAmount < grossAmount)
                    {
                        appliedPromo = promo.Code;
                        if (bookingStatus != "CANCELLED")
                        {
                            connection.Execute(
                                "UPDATE Promotions SET UsedCount = UsedCount + 1 WHERE Id = @Id;",
                                new { promo.Id },
                                tx);
                        }
                    }
                    else
                    {
                        discountAmount = 0m;
                    }
                }

                decimal finalAmount = grossAmount - discountAmount;

                long bookingId = connection.ExecuteScalar<long>(@"
                    INSERT INTO Bookings (
                        UserId, BookingCode, BookingTime,
                        TotalAmount, DiscountAmount, FinalAmount,
                        PaymentMethod, PaymentStatus, BookingStatus,
                        PromotionCode, Channel, CustomerName
                    )
                    VALUES (
                        @UserId, @BookingCode, @BookingTime,
                        @TotalAmount, @DiscountAmount, @FinalAmount,
                        @PaymentMethod, @PaymentStatus, @BookingStatus,
                        @PromotionCode, @Channel, @CustomerName
                    );
                    SELECT last_insert_rowid();
                ", new
                {
                    UserId = userId,
                    BookingCode = bookingCode,
                    BookingTime = FmtDateTime(bookingTime),
                    TotalAmount = grossAmount,
                    DiscountAmount = discountAmount,
                    FinalAmount = finalAmount,
                    PaymentMethod = paymentMethod,
                    PaymentStatus = paymentStatus,
                    BookingStatus = bookingStatus,
                    PromotionCode = appliedPromo,
                    Channel = channel,
                    CustomerName = customerName
                }, tx);

                foreach (var sp in seatPrices)
                {
                    connection.Execute(@"
                        INSERT INTO Tickets (
                            BookingId, ShowtimeId, SeatId,
                            TicketCode, SeatNumber, Price, Status, CreatedAt
                        )
                        VALUES (
                            @BookingId, @ShowtimeId, @SeatId,
                            @TicketCode, @SeatNumber, @Price, @Status, @CreatedAt
                        );
                    ", new
                    {
                        BookingId = bookingId,
                        ShowtimeId = showtime.Id,
                        SeatId = sp.Seat.Id,
                        TicketCode = $"{bookingCode}-{sp.Seat.SeatNumber}",
                        SeatNumber = sp.Seat.SeatNumber,
                        Price = sp.Price,
                        Status = ticketStatus,
                        CreatedAt = FmtDateTime(bookingTime)
                    }, tx);
                }

                if (foodItems != null)
                {
                    foreach (var fi in foodItems)
                    {
                        connection.Execute(@"
                            INSERT INTO BookingDetailProducts (
                                BookingId, ProductId, ProductName, Quantity, UnitPrice, TotalPrice
                            )
                            VALUES (
                                @BookingId, @ProductId, @ProductName, @Quantity, @UnitPrice, @TotalPrice
                            );
                        ", new
                        {
                            BookingId = bookingId,
                            ProductId = fi.Product.Id,
                            ProductName = fi.Product.Name,
                            Quantity = fi.Qty,
                            UnitPrice = fi.Product.Price,
                            TotalPrice = fi.Product.Price * fi.Qty
                        }, tx);
                    }
                }

                if (userId.HasValue && paymentStatus == "PAID" && bookingStatus != "CANCELLED")
                {
                    int pts = (int)Math.Floor(finalAmount / 1000m);
                    if (pts > 0)
                    {
                        connection.Execute(
                            "UPDATE Users SET RewardPoints = RewardPoints + @Pts WHERE Id = @UserId;",
                            new { Pts = pts, UserId = userId.Value },
                            tx);
                    }
                }

                return bookingId;
            }

            string[] paymentMethods =
            {
                "QR Pay Ngân Hàng (VietQR)",
                "Ví Điện Tử MoMo",
                "Thẻ Quốc Tế / ATM",
                "Tiền mặt"
            };

            int[] customerIds =
            {
                userIdByUsername["khachhang"],
                userIdByUsername["nguyenvana"],
                userIdByUsername["tranthib"],
                userIdByUsername["leminhc"],
                userIdByUsername["hoangnam"],
                userIdByUsername["vuthithao"]
            };

            // 8.1 Dữ liệu đơn hàng lịch sử (-45 đến -1 ngày) giúp biểu đồ Doanh thu, Phòng, Phim, Khung giờ sống động
            for (int dayOffset = -45; dayOffset <= -1; dayOffset++)
            {
                if (!pastShowtimesByOffset.TryGetValue(dayOffset, out var dayShows) || dayShows.Count == 0)
                    continue;

                // Tăng dần lượng vé ở kỳ gần đây (-29..-1) so với kỳ trước (-45..-30) để KPI tăng trưởng xanh đẹp mắt
                int ordersPerDay = dayOffset >= -7 ? 4 : (dayOffset >= -29 ? 3 : 2);

                for (int k = 0; k < ordersPerDay; k++)
                {
                    bookingSeq++;
                    Showtime st = dayShows[k % dayShows.Count];
                    int custId = customerIds[(Math.Abs(dayOffset) + k) % customerIds.Length];
                    string custName = fullNameByUserId[custId];
                    string payMethod = paymentMethods[(Math.Abs(dayOffset) + k) % paymentMethods.Length];
                    string channel = payMethod == "Tiền mặt" ? "POS" : "Online";
                    string prefix = channel == "POS" ? "POS" : "BK";
                    int seatCount = 2 + ((Math.Abs(dayOffset) + k) % 4); // 2..5 ghế/đơn
                    int preferredRow = 2 + (k % 4);

                    var foods = new List<(Product, int)>();
                    if (k % 2 == 0)
                    {
                        foods.Add((productsSeed[6 + (k % 3)], 1)); // Combo Solo / Couple / Family
                    }
                    else if (k % 3 == 1)
                    {
                        foods.Add((productsSeed[0], 1));
                        foods.Add((productsSeed[3], 2));
                    }

                    DateTime bookTime = st.StartTime.AddHours(-2).AddMinutes(-(k * 13));
                    string code = $"{prefix}{st.StartTime:yyyyMMdd}{bookingSeq}";

                    // Tránh gán mã khuyến mãi cho tài khoản khachhang để user test nhập mọi mã ở Checkout
                    string? promo = null;
                    if (custId != userIdByUsername["khachhang"] && dayOffset == -5 && k == 0)
                        promo = "HELLO2026";
                    else if (custId != userIdByUsername["khachhang"] && dayOffset == -3 && k == 1)
                        promo = "CINEMA30K";

                    InsertSeededBooking(
                        bookingCode: code,
                        userId: channel == "POS" ? null : custId,
                        customerName: channel == "POS" ? "Khách lẻ tại quầy" : custName,
                        showtime: st,
                        seatCount: seatCount,
                        preferredRowStart: preferredRow,
                        foodItems: foods,
                        paymentMethod: payMethod,
                        channel: channel,
                        bookingTime: bookTime,
                        ticketStatus: "USED",
                        bookingStatus: "CONFIRMED",
                        paymentStatus: "PAID",
                        promoCode: promo);
                }

                // Tạo một vài đơn hủy rải rác để biểu đồ "Vé bán & Vé hủy" hiển thị trực quan
                if (dayOffset is -20 or -11 or -6 or -2)
                {
                    bookingSeq++;
                    Showtime stCancel = dayShows[0];
                    InsertSeededBooking(
                        bookingCode: $"BK{stCancel.StartTime:yyyyMMdd}{bookingSeq}",
                        userId: userIdByUsername["nguyenvana"],
                        customerName: "Nguyễn Văn An",
                        showtime: stCancel,
                        seatCount: 2,
                        preferredRowStart: 1,
                        foodItems: null,
                        paymentMethod: "Ví Điện Tử MoMo",
                        channel: "Online",
                        bookingTime: stCancel.StartTime.AddHours(-3),
                        ticketStatus: "CANCELLED",
                        bookingStatus: "CANCELLED",
                        paymentStatus: "REFUNDED");
                }
            }

            // 8.2 Các đơn đặt vé HÔM NAY & TƯƠNG LAI + các mã TEST soát vé cực kỳ tiện lợi
            var todayShows = futureShowtimesByOffset[0];
            var tomorrowShows = futureShowtimesByOffset[1];

            // Chọn các suất chắc chắn chưa kết thúc (ưu tiên suất sắp chiếu > now, fallback suất muộn nhất trong ngày)
            var upcomingToday = todayShows
                .Where(s => s.StartTime > now.AddMinutes(10))
                .OrderBy(s => s.StartTime)
                .ToList();

            Showtime targetShow1 = upcomingToday.FirstOrDefault() ?? tomorrowShows[2];
            Showtime targetShow2 = upcomingToday.Skip(1).FirstOrDefault() ?? tomorrowShows[4];
            Showtime targetShow3 = upcomingToday.Skip(3).FirstOrDefault() ?? tomorrowShows[5];
            Showtime expiredShow = pastShowtimesByOffset[-1].Last();

            // (A) Mã test 1: BK-TEST-VALID1 — Vé HỢP LỆ CHƯA SOÁT của tài khoản `khachhang` (hiển thị ngay ở Vé của tôi & Soát vé)
            InsertSeededBooking(
                bookingCode: "BK-TEST-VALID1",
                userId: userIdByUsername["khachhang"],
                customerName: "Phạm Quốc Bảo",
                showtime: targetShow1,
                seatCount: 2,
                preferredRowStart: 4, // Hàng E (VIP)
                foodItems: new List<(Product, int)> { (productsSeed[7], 1) }, // Combo Couple
                paymentMethod: "QR Pay Ngân Hàng (VietQR)",
                channel: "Online",
                bookingTime: now.AddMinutes(-45),
                ticketStatus: "BOOKED",
                bookingStatus: "CONFIRMED",
                paymentStatus: "PAID");

            // (B) Mã test 2: BK-TEST-VALID2 — Vé HỢP LỆ CHƯA SOÁT (Khách Nguyễn Văn An)
            InsertSeededBooking(
                bookingCode: "BK-TEST-VALID2",
                userId: userIdByUsername["nguyenvana"],
                customerName: "Nguyễn Văn An",
                showtime: targetShow2,
                seatCount: 3,
                preferredRowStart: 3, // Hàng D (VIP)
                foodItems: new List<(Product, int)> { (productsSeed[0], 1), (productsSeed[3], 2) },
                paymentMethod: "Ví Điện Tử MoMo",
                channel: "Online",
                bookingTime: now.AddMinutes(-30),
                ticketStatus: "BOOKED",
                bookingStatus: "CONFIRMED",
                paymentStatus: "PAID");

            // (C) Mã test 3: POS-TEST-001 — Đơn bán tại quầy POS CHƯA SOÁT
            InsertSeededBooking(
                bookingCode: "POS-TEST-001",
                userId: null,
                customerName: "Anh Tuấn (Khách quầy)",
                showtime: targetShow3,
                seatCount: 2,
                preferredRowStart: 2,
                foodItems: new List<(Product, int)> { (productsSeed[6], 2) },
                paymentMethod: "Tiền mặt",
                channel: "POS",
                bookingTime: now.AddMinutes(-20),
                ticketStatus: "BOOKED",
                bookingStatus: "CONFIRMED",
                paymentStatus: "PAID");

            // (D) Mã test 4: BK-TEST-USED01 — Vé ĐÃ SOÁT (test cảnh báo "VÉ ĐÃ ĐƯỢC SỬ DỤNG")
            InsertSeededBooking(
                bookingCode: "BK-TEST-USED01",
                userId: userIdByUsername["leminhc"],
                customerName: "Lê Minh Châu",
                showtime: targetShow1,
                seatCount: 2,
                preferredRowStart: 3,
                foodItems: new List<(Product, int)> { (productsSeed[1], 1) },
                paymentMethod: "Thẻ Quốc Tế / ATM",
                channel: "Online",
                bookingTime: now.AddMinutes(-90),
                ticketStatus: "USED",
                bookingStatus: "CONFIRMED",
                paymentStatus: "PAID");

            // (E) Mã test 5: BK-TEST-EXPIRED — Vé QUÁ HẠN chưa soát của ngày hôm qua (test cảnh báo "VÉ HẾT HẠN")
            InsertSeededBooking(
                bookingCode: "BK-TEST-EXPIRED",
                userId: userIdByUsername["tranthib"],
                customerName: "Trần Thị Bích Ngọc",
                showtime: expiredShow,
                seatCount: 2,
                preferredRowStart: 1,
                foodItems: null,
                paymentMethod: "QR Pay Ngân Hàng (VietQR)",
                channel: "Online",
                bookingTime: expiredShow.StartTime.AddHours(-4),
                ticketStatus: "BOOKED",
                bookingStatus: "CONFIRMED",
                paymentStatus: "PAID");

            // (F) Đặt sẵn một số ghế rải rác ở các suất sắp chiếu hôm nay & ngày mai để sơ đồ ghế sinh động khi chọn ghế
            var sampleUpcomingList = upcomingToday.Take(6).Concat(tomorrowShows.Take(6)).ToList();
            for (int i = 0; i < sampleUpcomingList.Count; i++)
            {
                bookingSeq++;
                Showtime st = sampleUpcomingList[i];
                int custId = customerIds[(i + 2) % customerIds.Length];
                InsertSeededBooking(
                    bookingCode: $"BK{today:yyyyMMdd}{bookingSeq}",
                    userId: custId,
                    customerName: fullNameByUserId[custId],
                    showtime: st,
                    seatCount: 3 + (i % 3),
                    preferredRowStart: 2 + (i % 3),
                    foodItems: i % 2 == 0 ? new List<(Product, int)> { (productsSeed[6], 1) } : null,
                    paymentMethod: paymentMethods[i % paymentMethods.Length],
                    channel: "Online",
                    bookingTime: now.AddMinutes(-(10 + i * 7)),
                    ticketStatus: "BOOKED",
                    bookingStatus: "CONFIRMED",
                    paymentStatus: "PAID");
            }

            tx.Commit();
        }
        finally
        {
            connection.Execute("PRAGMA foreign_keys = ON;");
        }
    }

    /// <summary>
    /// Xuất toàn bộ dữ liệu từ SQLite ra file kịch bản SQL (.sql) để có thể chạy/tham khảo trên DataGrip, Rider, DB Browser.
    /// </summary>
    public static void ExportDatabaseToSqlFile(SqliteConnection connection, string outputPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("-- =============================================================================");
        sb.AppendLine("-- DỮ LIỆU MẪU KIỂM THỬ HỆ THỐNG QUẢN LÝ RẠP CHIẾU PHIM (CINEMA MANAGEMENT)");
        sb.AppendLine($"-- Ngày tạo: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-- Cơ sở dữ liệu: SQLite (cinema.db)");
        sb.AppendLine("-- =============================================================================");
        sb.AppendLine();
        sb.AppendLine("PRAGMA foreign_keys = OFF;");
        sb.AppendLine("BEGIN TRANSACTION;");
        sb.AppendLine();
        sb.AppendLine("DELETE FROM BookingDetailProducts;");
        sb.AppendLine("DELETE FROM Tickets;");
        sb.AppendLine("DELETE FROM Bookings;");
        sb.AppendLine("DELETE FROM Showtimes;");
        sb.AppendLine("DELETE FROM Seats;");
        sb.AppendLine("DELETE FROM Rooms;");
        sb.AppendLine("DELETE FROM PricingRules;");
        sb.AppendLine("DELETE FROM Promotions;");
        sb.AppendLine("DELETE FROM Products;");
        sb.AppendLine("DELETE FROM Movies;");
        sb.AppendLine("DELETE FROM Users;");
        sb.AppendLine("DELETE FROM sqlite_sequence;");
        sb.AppendLine();

        string[] tables =
        {
            "Users",
            "Movies",
            "Rooms",
            "Seats",
            "Products",
            "PricingRules",
            "Promotions",
            "Showtimes",
            "Bookings",
            "Tickets",
            "BookingDetailProducts"
        };

        foreach (string table in tables)
        {
            sb.AppendLine($"-- Bảng: {table}");
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT * FROM \"{table}\" ORDER BY Id;";
            using var reader = cmd.ExecuteReader();
            int fieldCount = reader.FieldCount;
            string[] cols = Enumerable.Range(0, fieldCount).Select(reader.GetName).ToArray();
            string colList = string.Join(", ", cols.Select(c => $"\"{c}\""));

            while (reader.Read())
            {
                string[] vals = new string[fieldCount];
                for (int i = 0; i < fieldCount; i++)
                {
                    if (reader.IsDBNull(i))
                    {
                        vals[i] = "NULL";
                    }
                    else
                    {
                        object raw = reader.GetValue(i);
                        vals[i] = raw switch
                        {
                            long or int or short or byte => Convert.ToString(raw, CultureInfo.InvariantCulture)!,
                            double or float or decimal => Convert.ToString(raw, CultureInfo.InvariantCulture)!,
                            _ => $"'{ (raw.ToString() ?? string.Empty).Replace("'", "''") }'"
                        };
                    }
                }

                sb.AppendLine($"INSERT INTO \"{table}\" ({colList}) VALUES ({string.Join(", ", vals)});");
            }

            sb.AppendLine();
        }

        sb.AppendLine("COMMIT;");
        sb.AppendLine("PRAGMA foreign_keys = ON;");

        string? dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(outputPath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static string FmtDateTime(DateTime dt) =>
        dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
}
