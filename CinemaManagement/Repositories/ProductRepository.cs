
using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using CinemaManagement.Data;
using CinemaManagement.Models;

namespace CinemaManagement.Repositories;

public class ProductRepository
{
    // 1. LẤY TOÀN BỘ SẢN PHẨM
    public List<Product> GetAll()
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            SELECT
                Id,
                Name,
                Category,
                Price,
                StockQuantity,
                ImageUrl,
                IsActive
            FROM Products
            ORDER BY Id DESC;";

        return connection.Query<Product>(sql).ToList();
    }

    // 2. LẤY SẢN PHẨM THEO ID
    public Product? GetById(int id)
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            SELECT
                Id,
                Name,
                Category,
                Price,
                StockQuantity,
                ImageUrl,
                IsActive
            FROM Products
            WHERE Id = @Id;";

        return connection.QueryFirstOrDefault<Product>(
            sql,
            new { Id = id }
        );
    }

    // 3. KIỂM TRA TÊN SẢN PHẨM TRÙNG
    public bool ExistsByName(string name, int? excludeId = null)
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            SELECT COUNT(1)
            FROM Products
            WHERE LOWER(Name) = LOWER(@Name)
              AND (@ExcludeId IS NULL OR Id <> @ExcludeId);";

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

    // 4. THÊM SẢN PHẨM
    public int Add(Product product)
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            INSERT INTO Products
            (
                Name,
                Category,
                Price,
                StockQuantity,
                ImageUrl,
                IsActive
            )
            VALUES
            (
                @Name,
                @Category,
                @Price,
                @StockQuantity,
                @ImageUrl,
                @IsActive
            );

            SELECT last_insert_rowid();";

        return connection.ExecuteScalar<int>(sql, product);
    }

    // 5. CẬP NHẬT SẢN PHẨM
    public bool Update(Product product)
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            UPDATE Products
            SET
                Name = @Name,
                Category = @Category,
                Price = @Price,
                StockQuantity = @StockQuantity,
                ImageUrl = @ImageUrl,
                IsActive = @IsActive
            WHERE Id = @Id;";

        return connection.Execute(sql, product) > 0;
    }

    // 6. XÓA SẢN PHẨM
    public bool Delete(int id)
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            DELETE FROM Products
            WHERE Id = @Id;";

        return connection.Execute(sql, new { Id = id }) > 0;
    }

    // 7. NHẬP KHO
    public bool ImportStock(int id, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException(
                "Số lượng nhập kho phải lớn hơn 0.");

        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            UPDATE Products
            SET StockQuantity = StockQuantity + @Quantity
            WHERE Id = @Id
              AND StockQuantity <= @MaxStock;";

        return connection.Execute(sql, new
        {
            Id = id,
            Quantity = quantity,
            MaxStock = int.MaxValue - quantity
        }) > 0;
    }

    // 8. XUẤT KHO
    public bool ExportStock(int id, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException(
                "Số lượng xuất kho phải lớn hơn 0.");

        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            UPDATE Products
            SET StockQuantity = StockQuantity - @Quantity
            WHERE Id = @Id
              AND StockQuantity >= @Quantity;";

        return connection.Execute(sql, new
        {
            Id = id,
            Quantity = quantity
        }) > 0;
    }

    // 9. ĐẾM TỔNG SẢN PHẨM
    public int CountAll()
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            SELECT COUNT(*)
            FROM Products;";

        return connection.ExecuteScalar<int>(sql);
    }

    // 10. ĐẾM SẢN PHẨM ĐANG KINH DOANH
    public int CountActive()
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            SELECT COUNT(*)
            FROM Products
            WHERE IsActive = 1;";

        return connection.ExecuteScalar<int>(sql);
    }

    // 11. ĐẾM SẢN PHẨM SẮP HẾT HÀNG
    public int CountLowStock()
    {
        using var connection = DatabaseConnection.GetConnection();

        const string sql = @"
            SELECT COUNT(*)
            FROM Products
            WHERE StockQuantity <= 10
              AND IsActive = 1;";

        return connection.ExecuteScalar<int>(sql);
    }
}
