
using System;
using System.Collections.Generic;
using CinemaManagement.Models;
using CinemaManagement.Repositories;

namespace CinemaManagement.Services;

public class PricingService
{
    private readonly PricingRepository _repository;

    public PricingService()
    {
        _repository = new PricingRepository();
    }

    // Lấy toàn bộ quy tắc giá
    public List<PricingRule> GetAll()
    {
        return _repository.GetAll();
    }

    // Lấy quy tắc giá theo ID
    public PricingRule? GetById(int id)
    {
        if (id <= 0)
            throw new ArgumentException("Mã quy tắc giá không hợp lệ.");

        return _repository.GetById(id);
    }

    // Kiểm tra dữ liệu quy tắc giá
    private void Validate(PricingRule rule)
    {
        if (rule == null)
            throw new ArgumentNullException(nameof(rule));

        if (string.IsNullOrWhiteSpace(rule.Format))
            throw new ArgumentException("Vui lòng chọn định dạng phim.");

        if (string.IsNullOrWhiteSpace(rule.SeatType))
            throw new ArgumentException("Vui lòng chọn loại ghế.");

        if (string.IsNullOrWhiteSpace(rule.DayType))
            throw new ArgumentException("Vui lòng chọn ngày áp dụng.");

        if (string.IsNullOrWhiteSpace(rule.TimeSlot))
            throw new ArgumentException("Vui lòng chọn khung giờ.");

        if (rule.BasePrice <= 0)
            throw new ArgumentException("Giá cơ bản phải lớn hơn 0.");

        if (rule.FinalPrice <= 0)
            throw new ArgumentException("Giá vé sau điều chỉnh phải lớn hơn 0.");
    }

    // Kiểm tra quy tắc bị trùng
    public bool IsDuplicate(PricingRule rule)
    {
        Validate(rule);
        return _repository.ExistsDuplicate(rule);
    }

    // Thêm quy tắc giá
    public int Add(PricingRule rule)
    {
        Validate(rule);

        if (_repository.ExistsDuplicate(rule))
            throw new InvalidOperationException(
                "Quy tắc giá này đã tồn tại.");

        return _repository.Add(rule);
    }

    // Cập nhật quy tắc giá
    public bool Update(PricingRule rule)
    {
        Validate(rule);

        if (rule.Id <= 0)
            throw new ArgumentException("Mã quy tắc giá không hợp lệ.");

        if (_repository.GetById(rule.Id) == null)
            throw new InvalidOperationException(
                "Không tìm thấy quy tắc giá cần cập nhật.");

        if (_repository.ExistsDuplicate(rule))
            throw new InvalidOperationException(
                "Quy tắc giá này đã tồn tại.");

        return _repository.Update(rule);
    }

    // Xóa quy tắc giá
    public bool Delete(int id)
    {
        if (id <= 0)
            throw new ArgumentException("Mã quy tắc giá không hợp lệ.");

        if (_repository.GetById(id) == null)
            throw new InvalidOperationException(
                "Không tìm thấy quy tắc giá cần xóa.");

        return _repository.Delete(id);
    }

    // Đếm tổng số quy tắc giá
    public int CountAll()
    {
        return _repository.CountAll();
    }
}
