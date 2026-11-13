
using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using CinemaManagement.Data;
using CinemaManagement.Models;

namespace CinemaManagement.Repositories;

public class RoomRepository
{
    // 1. Lấy toàn bộ phòng chiếu
    public List<Room> GetAll()
    {
        using var connection = DatabaseConnection.GetConnection();
        connection.Open();

        const string sql = @"
            SELECT
                Id,
                Name,
                TotalSeats,
                TotalRows,
                TotalColumns
            FROM Rooms
            ORDER BY Id;
        ";

        return connection.Query<Room>(sql).ToList();
    }

    // 2. Lấy phòng theo ID
    public Room? GetById(long id)
    {
        using var connection = DatabaseConnection.GetConnection();
        connection.Open();

        const string sql = @"
            SELECT
                Id,
                Name,
                TotalSeats,
                TotalRows,
                TotalColumns
            FROM Rooms
            WHERE Id = @Id;
        ";

        return connection.QueryFirstOrDefault<Room>(
            sql,
            new { Id = id }
        );
    }

    // 3. Lấy danh sách ghế của một phòng
    public List<Seat> GetSeatsByRoomId(long roomId)
    {
        using var connection = DatabaseConnection.GetConnection();
        connection.Open();

        const string sql = @"
            SELECT
                Id,
                RoomId,
                SeatNumber,
                RowIndex,
                ColumnIndex,
                Type,
                PriceMultiplier
            FROM Seats
            WHERE RoomId = @RoomId
            ORDER BY RowIndex, ColumnIndex;
        ";

        return connection.Query<Seat>(
            sql,
            new { RoomId = roomId }
        ).ToList();
    }

    // 4. Thêm phòng và toàn bộ ghế trong một transaction
    public long Add(Room room, IEnumerable<Seat> roomSeats)
    {
        using var connection = DatabaseConnection.GetConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            const string insertRoomSql = @"
                INSERT INTO Rooms (
                    Name,
                    TotalSeats,
                    TotalRows,
                    TotalColumns
                )
                VALUES (
                    @Name,
                    @TotalSeats,
                    @TotalRows,
                    @TotalColumns
                );

                SELECT last_insert_rowid();
            ";

            long newRoomId = connection.ExecuteScalar<long>(
                insertRoomSql,
                room,
                transaction
            );

            InsertSeats(
                connection,
                transaction,
                newRoomId,
                roomSeats
            );

            transaction.Commit();

            return newRoomId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    // 5. Cập nhật thông tin phòng
    // replaceSeats = true khi thay đổi số hàng hoặc số cột
    public bool Update(
        Room room,
        IEnumerable<Seat> roomSeats,
        bool replaceSeats)
    {
        using var connection = DatabaseConnection.GetConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            const string updateRoomSql = @"
                UPDATE Rooms
                SET
                    Name = @Name,
                    TotalSeats = @TotalSeats,
                    TotalRows = @TotalRows,
                    TotalColumns = @TotalColumns
                WHERE Id = @Id;
            ";

            int rowsAffected = connection.Execute(
                updateRoomSql,
                room,
                transaction
            );

            if (rowsAffected == 0)
            {
                transaction.Rollback();
                return false;
            }

            if (replaceSeats)
            {
                connection.Execute(
                    "DELETE FROM Seats WHERE RoomId = @RoomId;",
                    new { RoomId = room.Id },
                    transaction
                );

                InsertSeats(
                    connection,
                    transaction,
                    room.Id,
                    roomSeats
                );
            }

            transaction.Commit();

            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    // 6. Cập nhật loại ghế và hệ số giá
    public bool UpdateSeat(Seat seat)
    {
        using var connection = DatabaseConnection.GetConnection();
        connection.Open();

        const string sql = @"
            UPDATE Seats
            SET
                Type = @Type,
                PriceMultiplier = @PriceMultiplier
            WHERE Id = @Id
              AND RoomId = @RoomId;
        ";

        int rowsAffected = connection.Execute(sql, new
        {
            seat.Id,
            seat.RoomId,
            Type = (int)seat.Type,
            seat.PriceMultiplier
        });

        return rowsAffected > 0;
    }

    // 7. Xóa phòng
    public bool Delete(long id)
    {
        using var connection = DatabaseConnection.GetConnection();
        connection.Open();

        const string sql = @"
            DELETE FROM Rooms
            WHERE Id = @Id;
        ";

        int rowsAffected = connection.Execute(
            sql,
            new { Id = id }
        );

        return rowsAffected > 0;
    }

    // 8. Kiểm tra tên phòng đã tồn tại
    public bool ExistsByName(string name, long? excludeId = null)
    {
        using var connection = DatabaseConnection.GetConnection();
        connection.Open();

        const string sql = @"
            SELECT COUNT(1)
            FROM Rooms
            WHERE LOWER(Name) = LOWER(@Name)
              AND (@ExcludeId IS NULL OR Id <> @ExcludeId);
        ";

        int count = connection.ExecuteScalar<int>(
            sql,
            new
            {
                Name = name.Trim(),
                ExcludeId = excludeId
            }
        );

        return count > 0;
    }

    // 9. Hàm dùng chung để thêm ghế
    private static void InsertSeats(
        Microsoft.Data.Sqlite.SqliteConnection connection,
        Microsoft.Data.Sqlite.SqliteTransaction transaction,
        long roomId,
        IEnumerable<Seat> roomSeats)
    {
        const string sql = @"
            INSERT INTO Seats (
                RoomId,
                SeatNumber,
                RowIndex,
                ColumnIndex,
                Type,
                PriceMultiplier
            )
            VALUES (
                @RoomId,
                @SeatNumber,
                @RowIndex,
                @ColumnIndex,
                @Type,
                @PriceMultiplier
            );
        ";

        var parameters = roomSeats.Select(seat => new
        {
            RoomId = roomId,
            seat.SeatNumber,
            seat.RowIndex,
            seat.ColumnIndex,
            Type = (int)seat.Type,
            seat.PriceMultiplier
        });

        connection.Execute(sql, parameters, transaction);
    }
}
