using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using CinemaManagement.Data;

namespace CinemaManagement.Repositories
{
    public class TicketRepository
    {
        /// <summary>
        /// Lấy danh sách số ghế đã được đặt theo suất chiếu.
        /// </summary>
        public HashSet<string> GetBookedSeatNumbers(long showtimeId)
        {
            if (showtimeId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(showtimeId),
                    "Mã suất chiếu phải lớn hơn 0.");
            }

            const string sql = @"
                SELECT DISTINCT SeatNumber
                FROM Tickets
                WHERE ShowtimeId = @ShowtimeId
                  AND UPPER(Status) <> 'CANCELLED';
            ";

            using var connection = DatabaseConnection.GetConnection();
            connection.Open();

            List<string> seats = connection.Query<string>(
                    sql,
                    new { ShowtimeId = showtimeId })
                .Where(seat => !string.IsNullOrWhiteSpace(seat))
                .ToList();

            return new HashSet<string>(
                (IEnumerable<string>)seats,
                StringComparer.OrdinalIgnoreCase);
        }
    }
}