
using System;
using System.Collections.Generic;
using System.Linq;
using CinemaManagement.Models;
using CinemaManagement.Models.Enums;
using CinemaManagement.Repositories;

namespace CinemaManagement.Services;

public class PricingService
{
    public const decimal DefaultBasePrice = 90000m;

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

    // =========================================
    // TÍNH GIÁ VÉ THỰC TẾ CHO LUỒNG ĐẶT VÉ
    // =========================================

    public static string GetSeatTypeName(SeatType seatType, double multiplier = 1.0)
    {
        if (seatType == SeatType.Couple ||
            (seatType == SeatType.Standard && multiplier >= 1.8))
        {
            return "Ghế đôi";
        }

        if (seatType == SeatType.VIP ||
            (seatType == SeatType.Standard && multiplier > 1.0))
        {
            return "Ghế VIP";
        }

        return "Ghế thường";
    }

    public static string GetDayType(DateTime startTime)
    {
        return startTime.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
            ? "Cuối tuần"
            : "Ngày thường";
    }

    public static string GetTimeSlot(DateTime startTime)
    {
        TimeSpan time = startTime.TimeOfDay;

        if (time < TimeSpan.FromHours(12))
            return "Trước 12:00";

        if (time < TimeSpan.FromHours(17))
            return "12:00 - 17:00";

        return "Sau 17:00";
    }

    /// <summary>
    /// Tính giá của một ghế cụ thể dựa trên bảng PricingRules (ưu tiên)
    /// hoặc fallback về Showtime.BasePrice * Seat.PriceMultiplier.
    /// </summary>
    public decimal CalculateSeatPrice(
        Showtime showtime,
        Seat seat,
        IReadOnlyCollection<PricingRule>? cachedRules = null)
    {
        if (showtime == null)
            throw new ArgumentNullException(nameof(showtime));

        if (seat == null)
            throw new ArgumentNullException(nameof(seat));

        return CalculatePriceBySeatType(
            showtime,
            seat.Type,
            seat.PriceMultiplier,
            cachedRules);
    }

    public decimal CalculatePriceBySeatType(
        Showtime showtime,
        SeatType seatType,
        double multiplier = 1.0,
        IReadOnlyCollection<PricingRule>? cachedRules = null)
    {
        if (showtime == null)
            throw new ArgumentNullException(nameof(showtime));

        string format = string.IsNullOrWhiteSpace(showtime.Format)
            ? "2D"
            : showtime.Format.Trim().ToUpperInvariant();

        string seatTypeName = GetSeatTypeName(seatType, multiplier);
        string dayType = GetDayType(showtime.StartTime);
        string timeSlot = GetTimeSlot(showtime.StartTime);

        IReadOnlyCollection<PricingRule> rules = cachedRules ?? GetAll();

        PricingRule? matchedRule = rules.FirstOrDefault(r =>
            string.Equals(r.Format?.Trim(), format, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.SeatType?.Trim(), seatTypeName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.DayType?.Trim(), dayType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.TimeSlot?.Trim(), timeSlot, StringComparison.OrdinalIgnoreCase));

        matchedRule ??= rules.FirstOrDefault(r =>
            string.Equals(r.Format?.Trim(), format, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.SeatType?.Trim(), seatTypeName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.DayType?.Trim(), dayType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.TimeSlot?.Trim(), "Cả ngày", StringComparison.OrdinalIgnoreCase));

        if (matchedRule != null && matchedRule.FinalPrice > 0)
        {
            return matchedRule.FinalPrice;
        }

        decimal basePrice = showtime.BasePrice > 0
            ? showtime.BasePrice
            : DefaultBasePrice;

        decimal safeMultiplier = multiplier > 0
            ? Convert.ToDecimal(multiplier)
            : 1.0m;

        return Math.Round(basePrice * safeMultiplier, 0);
    }
}
