using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using CinemaManagement.Data;
using CinemaManagement.Models;

namespace CinemaManagement.Repositories;

public class ShowtimeRepository
{
    // Lấy toàn bộ suất chiếu
    public List<Showtime> GetAll()
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            SELECT
                Id,
                MovieId,
                RoomId,
                StartTime,
                EndTime,
                BasePrice,
                Format
            FROM Showtimes
            ORDER BY StartTime ASC;
        ";

        return connection.Query<Showtime>(sql).AsList();
    }

    // Lấy suất chiếu theo ID
    public Showtime? GetById(int id)
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            SELECT
                Id,
                MovieId,
                RoomId,
                StartTime,
                EndTime,
                BasePrice,
                Format
            FROM Showtimes
            WHERE Id = @Id;
        ";

        return connection.QueryFirstOrDefault<Showtime>(
            sql,
            new { Id = id }
        );
    }

    // Lấy suất chiếu theo phòng
    public List<Showtime> GetByRoomId(int roomId)
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            SELECT
                Id,
                MovieId,
                RoomId,
                StartTime,
                EndTime,
                BasePrice,
                Format
            FROM Showtimes
            WHERE RoomId = @RoomId
            ORDER BY StartTime ASC;
        ";

        return connection.Query<Showtime>(
            sql,
            new { RoomId = roomId }
        ).AsList();
    }

    // Lấy suất chiếu theo phim
    public List<Showtime> GetByMovieId(int movieId)
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            SELECT
                Id,
                MovieId,
                RoomId,
                StartTime,
                EndTime,
                BasePrice,
                Format
            FROM Showtimes
            WHERE MovieId = @MovieId
            ORDER BY StartTime ASC;
        ";

        return connection.Query<Showtime>(
            sql,
            new { MovieId = movieId }
        ).AsList();
    }

    // Lấy các suất chiếu sắp diễn ra trong ngày (lọc trực tiếp tại SQLite)
    public List<Showtime> GetUpcomingByDate(
        DateTime date,
        DateTime afterTime,
        int? movieId = null)
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            SELECT
                Id,
                MovieId,
                RoomId,
                StartTime,
                EndTime,
                BasePrice,
                Format
            FROM Showtimes
            WHERE date(StartTime) = date(@Date)
              AND StartTime > @AfterTime
              AND (@MovieId IS NULL OR MovieId = @MovieId)
            ORDER BY StartTime ASC;
        ";

        return connection.Query<Showtime>(
            sql,
            new
            {
                Date = date.ToString("yyyy-MM-dd"),
                AfterTime = afterTime.ToString("yyyy-MM-dd HH:mm:ss"),
                MovieId = movieId
            }
        ).AsList();
    }

    // Thêm suất chiếu
    public int Add(Showtime showtime)
    {
        ValidateShowtime(showtime);

        using var connection = DatabaseConnection.GetConnection();
        connection.Open();

        const string sql = @"
            INSERT INTO Showtimes
            (
                MovieId,
                RoomId,
                StartTime,
                EndTime,
                BasePrice,
                Format
            )
            VALUES
            (
                @MovieId,
                @RoomId,
                @StartTime,
                @EndTime,
                @BasePrice,
                @Format
            );

            SELECT last_insert_rowid();
        ";

        int newId = connection.ExecuteScalar<int>(
            sql,
            new
            {
                showtime.MovieId,
                showtime.RoomId,
                StartTime = showtime.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                EndTime = showtime.EndTime.ToString("yyyy-MM-dd HH:mm:ss"),
                showtime.BasePrice,
                Format = NormalizeFormat(showtime.Format)
            }
        );

        showtime.Id = newId;
        showtime.Format = NormalizeFormat(showtime.Format);

        return newId;
    }

    // Cập nhật suất chiếu
    public bool Update(Showtime showtime)
    {
        ValidateShowtime(showtime);

        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            UPDATE Showtimes
            SET
                MovieId = @MovieId,
                RoomId = @RoomId,
                StartTime = @StartTime,
                EndTime = @EndTime,
                BasePrice = @BasePrice,
                Format = @Format
            WHERE Id = @Id;
        ";

        int affectedRows = connection.Execute(
            sql,
            new
            {
                showtime.Id,
                showtime.MovieId,
                showtime.RoomId,
                StartTime = showtime.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                EndTime = showtime.EndTime.ToString("yyyy-MM-dd HH:mm:ss"),
                showtime.BasePrice,
                Format = NormalizeFormat(showtime.Format)
            }
        );

        return affectedRows > 0;
    }

    // Xóa suất chiếu
    public bool Delete(int id)
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            DELETE FROM Showtimes
            WHERE Id = @Id;
        ";

        int affectedRows = connection.Execute(
            sql,
            new { Id = id }
        );

        return affectedRows > 0;
    }

    // Kiểm tra trùng lịch trong cùng phòng
    public bool HasScheduleConflict(
        int roomId,
        DateTime startTime,
        DateTime endTime,
        int? excludeShowtimeId = null)
    {
        if (endTime <= startTime)
            throw new ArgumentException(
                "Giờ kết thúc phải sau giờ bắt đầu."
            );

        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            SELECT COUNT(1)
            FROM Showtimes
            WHERE RoomId = @RoomId
              AND (@ExcludeId IS NULL OR Id != @ExcludeId)
              AND StartTime < @EndTime
              AND EndTime > @StartTime;
        ";

        int count = connection.ExecuteScalar<int>(
            sql,
            new
            {
                RoomId = roomId,
                StartTime = startTime.ToString("yyyy-MM-dd HH:mm:ss"),
                EndTime = endTime.ToString("yyyy-MM-dd HH:mm:ss"),
                ExcludeId = excludeShowtimeId
            }
        );

        return count > 0;
    }

    // Đếm tổng số suất chiếu
    public int CountAll()
    {
        using var connection = DatabaseConnection.GetConnection();

        return connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM Showtimes;"
        );
    }

    // Đếm số suất chiếu trong ngày
    public int CountByDate(DateTime date)
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            SELECT COUNT(*)
            FROM Showtimes
            WHERE date(StartTime) = date(@Date);
        ";

        return connection.ExecuteScalar<int>(
            sql,
            new { Date = date.ToString("yyyy-MM-dd") }
        );
    }

    // Chuẩn hóa định dạng
    private static string NormalizeFormat(string? format)
    {
        string normalized = (format ?? "2D")
            .Trim()
            .ToUpperInvariant();

        if (normalized != "2D" && normalized != "3D")
            throw new ArgumentException(
                "Định dạng phim chỉ được phép là 2D hoặc 3D."
            );

        return normalized;
    }

    // Kiểm tra dữ liệu trước khi ghi
    private static void ValidateShowtime(Showtime showtime)
    {
        if (showtime == null)
            throw new ArgumentNullException(nameof(showtime));

        if (showtime.MovieId <= 0)
            throw new ArgumentException("Phim không hợp lệ.");

        if (showtime.RoomId <= 0)
            throw new ArgumentException("Phòng chiếu không hợp lệ.");

        if (showtime.EndTime <= showtime.StartTime)
            throw new ArgumentException(
                "Giờ kết thúc phải sau giờ bắt đầu."
            );

        if (showtime.BasePrice < 0)
            throw new ArgumentException(
                "Giá vé không được nhỏ hơn 0."
            );

        NormalizeFormat(showtime.Format);
    }
}