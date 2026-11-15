
using System;
using System.Collections.Generic;
using CinemaManagement.Models;
using CinemaManagement.Repositories;

namespace CinemaManagement.Services;

public class FoodService
{
    private readonly ProductRepository _repository;

    public FoodService()
    {
        _repository = new ProductRepository();
    }

    // LẤY DANH SÁCH SẢN PHẨM
    public List<Product> GetAll()
    {
        return _repository.GetAll();
    }

    // LẤY SẢN PHẨM THEO ID
    public Product? GetById(int id)
    {
        if (id <= 0)
            throw new ArgumentException("Mã sản phẩm không hợp lệ.");

        return _repository.GetById(id);
    }

    // KIỂM TRA DỮ LIỆU SẢN PHẨM
    private void Validate(Product product)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        if (string.IsNullOrWhiteSpace(product.Name))
            throw new ArgumentException("Vui lòng nhập tên sản phẩm.");

        if (string.IsNullOrWhiteSpace(product.Category))
            throw new ArgumentException("Vui lòng chọn danh mục.");

        if (product.Category != "Food" &&
            product.Category != "Drink" &&
            product.Category != "Combo")
        {
            throw new ArgumentException(
                "Danh mục sản phẩm không hợp lệ.");
        }

        if (product.Price <= 0)
            throw new ArgumentException("Giá bán phải lớn hơn 0.");

        if (product.StockQuantity < 0)
            throw new ArgumentException(
                "Số lượng tồn kho không hợp lệ.");
    }

    // THÊM SẢN PHẨM
    public int Add(Product product)
    {
        Validate(product);

        if (_repository.ExistsByName(product.Name))
        {
            throw new InvalidOperationException(
                "Tên sản phẩm đã tồn tại.");
        }

        return _repository.Add(product);
    }

    // CẬP NHẬT SẢN PHẨM
    public bool Update(Product product)
    {
        Validate(product);

        if (product.Id <= 0)
            throw new ArgumentException(
                "Mã sản phẩm không hợp lệ.");

        if (_repository.GetById(product.Id) == null)
        {
            throw new InvalidOperationException(
                "Không tìm thấy sản phẩm cần cập nhật.");
        }

        if (_repository.ExistsByName(product.Name, product.Id))
        {
            throw new InvalidOperationException(
                "Tên sản phẩm đã tồn tại.");
        }

        return _repository.Update(product);
    }

    // XÓA SẢN PHẨM
    public bool Delete(int id)
    {
        if (id <= 0)
            throw new ArgumentException(
                "Mã sản phẩm không hợp lệ.");

        if (_repository.GetById(id) == null)
        {
            throw new InvalidOperationException(
                "Không tìm thấy sản phẩm cần xóa.");
        }

        return _repository.Delete(id);
    }

    // NHẬP KHO
    public bool ImportStock(int id, int quantity)
    {
        if (id <= 0)
            throw new ArgumentException(
                "Mã sản phẩm không hợp lệ.");

        if (quantity <= 0)
            throw new ArgumentException(
                "Số lượng nhập phải lớn hơn 0.");

        if (_repository.GetById(id) == null)
        {
            throw new InvalidOperationException(
                "Không tìm thấy sản phẩm.");
        }

        return _repository.ImportStock(id, quantity);
    }

    // XUẤT KHO
    public bool ExportStock(int id, int quantity)
    {
        if (id <= 0)
            throw new ArgumentException(
                "Mã sản phẩm không hợp lệ.");

        if (quantity <= 0)
            throw new ArgumentException(
                "Số lượng xuất phải lớn hơn 0.");

        Product? product = _repository.GetById(id);

        if (product == null)
        {
            throw new InvalidOperationException(
                "Không tìm thấy sản phẩm.");
        }

        if (quantity > product.StockQuantity)
        {
            throw new InvalidOperationException(
                "Số lượng xuất lớn hơn số lượng tồn kho.");
        }

        return _repository.ExportStock(id, quantity);
    }

    // THỐNG KÊ TỔNG SẢN PHẨM
    public int CountAll()
    {
        return _repository.CountAll();
    }

    // THỐNG KÊ SẢN PHẨM ĐANG KINH DOANH
    public int CountActive()
    {
        return _repository.CountActive();
    }

    // THỐNG KÊ SẢN PHẨM SẮP HẾT HÀNG
    public int CountLowStock()
    {
        return _repository.CountLowStock();
    }

    // TRỪ TỒN KHO SAU KHI ĐẶT HÀNG
    public bool DeductStockForOrder(
        Dictionary<int, (string Name, decimal Price, int Qty, string ImgPath)> foodCart)
    {
        if (foodCart == null)
            throw new ArgumentNullException(nameof(foodCart));

        foreach (var item in foodCart)
        {
            if (item.Key <= 0 || item.Value.Qty <= 0)
            {
                throw new ArgumentException(
                    "Thông tin sản phẩm trong đơn hàng không hợp lệ.");
            }
        }

        return _repository.DeductStockForOrder(foodCart);
    }
}
