
using System;
using System.Collections.Generic;
using Dapper;
using CinemaManagement.Data;
using CinemaManagement.Models;

namespace CinemaManagement.Repositories;

public class ShowtimeRepository
{
    // Lấy toàn bộ danh sách suất chiếu
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
                BasePrice
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
                BasePrice
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
                BasePrice
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
                BasePrice
            FROM Showtimes
            WHERE MovieId = @MovieId
            ORDER BY StartTime ASC;
        ";

        return connection.Query<Showtime>(
            sql,
            new { MovieId = movieId }
        ).AsList();
    }

    // Thêm suất chiếu
    public int Add(Showtime showtime)
    {
        using var connection = DatabaseConnection.GetConnection();
        connection.Open();

        const string sql = @"
            INSERT INTO Showtimes
            (
                MovieId,
                RoomId,
                StartTime,
                EndTime,
                BasePrice
            )
            VALUES
            (
                @MovieId,
                @RoomId,
                @StartTime,
                @EndTime,
                @BasePrice
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
                showtime.BasePrice
            }
        );

        showtime.Id = newId;

        return newId;
    }

    // Cập nhật suất chiếu
    public bool Update(Showtime showtime)
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            UPDATE Showtimes
            SET
                MovieId = @MovieId,
                RoomId = @RoomId,
                StartTime = @StartTime,
                EndTime = @EndTime,
                BasePrice = @BasePrice
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
                showtime.BasePrice
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
}
