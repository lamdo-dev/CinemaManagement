using Dapper;
using CinemaManagement.Data;
using CinemaManagement.Models;

namespace CinemaManagement.Repositories;

public class PromotionRepository
{
    public List<Promotion> GetAll()
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            SELECT
                Id,
                Code,
                Name,
                DiscountType,
                DiscountValue,
                MaxDiscount,
                MinimumOrder,
                COALESCE(UsageLimit, 100) AS UsageLimit,
                COALESCE(UsedCount, 0) AS UsedCount,
                StartDate,
                EndDate,
                IsEnabled
            FROM Promotions
            ORDER BY Id DESC;";

        return connection.Query<Promotion>(sql).ToList();
    }

    public Promotion? GetById(int id)
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            SELECT
                Id,
                Code,
                Name,
                DiscountType,
                DiscountValue,
                MaxDiscount,
                MinimumOrder,
                COALESCE(UsageLimit, 100) AS UsageLimit,
                COALESCE(UsedCount, 0) AS UsedCount,
                StartDate,
                EndDate,
                IsEnabled
            FROM Promotions
            WHERE Id = @Id;";

        return connection.QueryFirstOrDefault<Promotion>(
            sql,
            new { Id = id }
        );
    }

    public bool ExistsByCode(string code, int excludeId = 0)
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            SELECT COUNT(1)
            FROM Promotions
            WHERE Code = @Code
              AND Id <> @ExcludeId;";

        return connection.ExecuteScalar<int>(
            sql,
            new
            {
                Code = code.Trim(),
                ExcludeId = excludeId
            }
        ) > 0;
    }

    public int Add(Promotion promotion)
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            INSERT INTO Promotions
            (
                Code,
                Name,
                DiscountType,
                DiscountValue,
                MaxDiscount,
                MinimumOrder,
                UsageLimit,
                UsedCount,
                StartDate,
                EndDate,
                IsEnabled
            )
            VALUES
            (
                @Code,
                @Name,
                @DiscountType,
                @DiscountValue,
                @MaxDiscount,
                @MinimumOrder,
                @UsageLimit,
                0,
                @StartDate,
                @EndDate,
                @IsEnabled
            );

            SELECT last_insert_rowid();";

        return connection.ExecuteScalar<int>(sql, new
        {
            promotion.Code,
            promotion.Name,
            promotion.DiscountType,
            promotion.DiscountValue,
            promotion.MaxDiscount,
            promotion.MinimumOrder,
            promotion.UsageLimit,
            StartDate = promotion.StartDate.ToString("yyyy-MM-dd"),
            EndDate = promotion.EndDate.ToString("yyyy-MM-dd"),
            promotion.IsEnabled
        });
    }

    public bool Update(Promotion promotion)
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            UPDATE Promotions
            SET
                Code = @Code,
                Name = @Name,
                DiscountType = @DiscountType,
                DiscountValue = @DiscountValue,
                MaxDiscount = @MaxDiscount,
                MinimumOrder = @MinimumOrder,
                UsageLimit = @UsageLimit,
                StartDate = @StartDate,
                EndDate = @EndDate,
                IsEnabled = @IsEnabled
            WHERE Id = @Id;";

        return connection.Execute(sql, new
        {
            promotion.Id,
            promotion.Code,
            promotion.Name,
            promotion.DiscountType,
            promotion.DiscountValue,
            promotion.MaxDiscount,
            promotion.MinimumOrder,
            promotion.UsageLimit,
            StartDate = promotion.StartDate.ToString("yyyy-MM-dd"),
            EndDate = promotion.EndDate.ToString("yyyy-MM-dd"),
            promotion.IsEnabled
        }) > 0;
    }

    public bool HasUserUsedPromotion(int userId, string code)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(code))
            return false;

        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            SELECT COUNT(1)
            FROM Bookings
            WHERE UserId = @UserId
              AND UPPER(PromotionCode) = UPPER(@Code)
              AND UPPER(BookingStatus) <> 'CANCELLED';";

        return connection.ExecuteScalar<int>(
            sql,
            new
            {
                UserId = userId,
                Code = code.Trim()
            }) > 0;
    }

    public bool Delete(int id)
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            DELETE FROM Promotions
            WHERE Id = @Id;";

        return connection.Execute(
            sql,
            new { Id = id }
        ) > 0;
    }

    public int CountAll()
    {
        using var connection = DatabaseConnection.GetConnection();

        return connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM Promotions;"
        );
    }
}