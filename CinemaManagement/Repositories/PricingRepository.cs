using System.Collections.Generic;
using System.Linq;
using Dapper;
using CinemaManagement.Data;
using CinemaManagement.Models;

namespace CinemaManagement.Repositories;

public class PricingRepository
{
    // Lấy toàn bộ quy tắc giá vé
    public List<PricingRule> GetAll()
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            SELECT
                Id,
                Format,
                SeatType,
                DayType,
                TimeSlot,
                BasePrice,
                Surcharge
            FROM PricingRules
            ORDER BY Id DESC;";

        return connection.Query<PricingRule>(sql).ToList();
    }

    // Lấy quy tắc giá theo ID
    public PricingRule? GetById(int id)
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            SELECT
                Id,
                Format,
                SeatType,
                DayType,
                TimeSlot,
                BasePrice,
                Surcharge
            FROM PricingRules
            WHERE Id = @Id;";

        return connection.QueryFirstOrDefault<PricingRule>(
            sql, new { Id = id });
    }

    // Kiểm tra quy tắc giá đã tồn tại hay chưa
    public bool ExistsDuplicate(PricingRule rule)
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            SELECT COUNT(1)
            FROM PricingRules
            WHERE Format = @Format
              AND SeatType = @SeatType
              AND DayType = @DayType
              AND TimeSlot = @TimeSlot
              AND Id <> @Id;";

        int count = connection.ExecuteScalar<int>(sql, new
        {
            rule.Format,
            rule.SeatType,
            rule.DayType,
            rule.TimeSlot,
            rule.Id
        });

        return count > 0;
    }

    // Thêm quy tắc giá mới
    public int Add(PricingRule rule)
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            INSERT INTO PricingRules
            (
                Format,
                SeatType,
                DayType,
                TimeSlot,
                BasePrice,
                Surcharge
            )
            VALUES
            (
                @Format,
                @SeatType,
                @DayType,
                @TimeSlot,
                @BasePrice,
                @Surcharge
            );

            SELECT last_insert_rowid();";

        return connection.ExecuteScalar<int>(sql, rule);
    }

    // Cập nhật quy tắc giá
    public bool Update(PricingRule rule)
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            UPDATE PricingRules
            SET
                Format = @Format,
                SeatType = @SeatType,
                DayType = @DayType,
                TimeSlot = @TimeSlot,
                BasePrice = @BasePrice,
                Surcharge = @Surcharge
            WHERE Id = @Id;";

        return connection.Execute(sql, rule) > 0;
    }

    // Xóa quy tắc giá
    public bool Delete(int id)
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            DELETE FROM PricingRules
            WHERE Id = @Id;";

        return connection.Execute(sql, new { Id = id }) > 0;
    }

    // Đếm tổng số quy tắc giá
    public int CountAll()
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            SELECT COUNT(*)
            FROM PricingRules;";

        return connection.ExecuteScalar<int>(sql);
    }
}
