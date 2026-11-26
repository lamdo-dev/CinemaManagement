
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
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

        string timeSlot = rule.TimeSlot.Trim();
        if (timeSlot.Contains('-') &&
            !TryParseTimeRange(timeSlot, out _, out _))
        {
            throw new ArgumentException(
                "Khung giờ tùy chỉnh phải có dạng HH:mm - HH:mm, " +
                "ví dụ 09:30 - 11:45. Giờ kết thúc phải sau giờ bắt đầu.");
        }

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
        DateTime date = startTime.Date;

        // Ngày lễ dương lịch cố định
        if (date.Month == 1 && date.Day == 1)
            return "Ngày lễ";

        if ((date.Month == 4 && date.Day == 30) ||
            (date.Month == 5 && date.Day == 1))
            return "30/4 - 1/5";

        if (date.Month == 9 && date.Day == 2)
            return "Quốc khánh 2/9";

        // Tết Nguyên Đán (ngày đầu năm và các ngày nghỉ liền kề).
        // Các ngày được khai báo theo lịch âm quy đổi cho từng năm.
        if (IsTetHoliday(date))
            return "Tết Nguyên Đán";

        // Giỗ Tổ Hùng Vương (10/3 âm lịch), quy đổi theo năm.
        if (IsHungKingsDay(date))
            return "Giỗ Tổ Hùng Vương";

        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return "Cuối tuần";

        return "Ngày thường";
    }

    private static bool IsTetHoliday(DateTime date)
    {
        // Bao gồm mùng 1 Tết và 4 ngày kế tiếp, cho các năm 2025-2030.
        DateTime[] tetStarts =
        {
            new(2025, 1, 29), new(2026, 2, 17), new(2027, 2, 6),
            new(2028, 1, 26), new(2029, 2, 13), new(2030, 2, 3)
        };

        return tetStarts.Any(start => date >= start && date <= start.AddDays(4));
    }

    private static bool IsHungKingsDay(DateTime date)
    {
        DateTime[] dates =
        {
            new(2025, 4, 7), new(2026, 4, 26), new(2027, 4, 16),
            new(2028, 4, 5), new(2029, 4, 24), new(2030, 4, 13)
        };

        return dates.Contains(date.Date);
    }

    public static string GetTimeSlot(DateTime startTime)
    {
        TimeSpan time = startTime.TimeOfDay;

        if (time < new TimeSpan(10, 0, 0))
            return "08:00 - 10:00";

        if (time < new TimeSpan(12, 0, 0))
            return "10:00 - 12:00";

        if (time < new TimeSpan(14, 0, 0))
            return "12:00 - 14:00";

        if (time < new TimeSpan(17, 0, 0))
            return "14:00 - 17:00";

        if (time < new TimeSpan(20, 0, 0))
            return "17:00 - 20:00";

        return "20:00 - 23:59";
    }

    private static string GetLegacyTimeSlot(DateTime startTime)
    {
        TimeSpan time = startTime.TimeOfDay;

        if (time < TimeSpan.FromHours(12))
            return "Trước 12:00";

        if (time < TimeSpan.FromHours(17))
            return "12:00 - 17:00";

        return "Sau 17:00";
    }

    private static bool TryParseTimeRange(
        string? value,
        out TimeSpan start,
        out TimeSpan end)
    {
        start = TimeSpan.Zero;
        end = TimeSpan.Zero;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        string[] parts = value.Split('-', StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
            return false;

        bool parsedStart = TimeSpan.TryParseExact(
            parts[0], new[] { @"h\:mm", @"hh\:mm" },
            CultureInfo.InvariantCulture, out start);

        bool parsedEnd = TimeSpan.TryParseExact(
            parts[1], new[] { @"h\:mm", @"hh\:mm" },
            CultureInfo.InvariantCulture, out end);

        return parsedStart && parsedEnd && start >= TimeSpan.Zero &&
               end <= TimeSpan.FromDays(1) && end > start;
    }

    /// <summary>
    /// Tính giá của một ghế cụ thể dựa trên bảng PricingRules (ưu tiên)
    /// hoặc fallback về Showtime.BasePrice * Seat.PriceMultiplier.
    /// </summary>
    private static bool IsHolidayDayType(string dayType) =>
        dayType is "Ngày lễ" or "Tết Nguyên Đán" or "Giỗ Tổ Hùng Vương" or
            "30/4 - 1/5" or "Quốc khánh 2/9";

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
        string legacyTimeSlot = GetLegacyTimeSlot(showtime.StartTime);

        IReadOnlyCollection<PricingRule> rules = cachedRules ?? GetAll();

        bool MatchesBase(PricingRule r) =>
            string.Equals(r.Format?.Trim(), format, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.SeatType?.Trim(), seatTypeName, StringComparison.OrdinalIgnoreCase);

        bool MatchesDay(PricingRule r) =>
            string.Equals(r.DayType?.Trim(), dayType, StringComparison.OrdinalIgnoreCase) ||
            (IsHolidayDayType(dayType) &&
             string.Equals(r.DayType?.Trim(), "Ngày lễ", StringComparison.OrdinalIgnoreCase));

        PricingRule? matchedRule = rules.FirstOrDefault(r =>
            MatchesBase(r) && MatchesDay(r) &&
            string.Equals(r.TimeSlot?.Trim(), timeSlot, StringComparison.OrdinalIgnoreCase));

        // Quy tắc giờ tự nhập, ví dụ 09:30 - 11:45.
        if (matchedRule == null)
        {
            TimeSpan showtimeTime = showtime.StartTime.TimeOfDay;
            matchedRule = rules.FirstOrDefault(r =>
            {
                if (!MatchesBase(r) || !MatchesDay(r) ||
                    !TryParseTimeRange(r.TimeSlot, out TimeSpan rangeStart, out TimeSpan rangeEnd))
                    return false;

                return showtimeTime >= rangeStart && showtimeTime < rangeEnd;
            });
        }

        // Tương thích các quy tắc cũ (Trước 12:00 / 12:00 - 17:00 / Sau 17:00).
        matchedRule ??= rules.FirstOrDefault(r =>
            MatchesBase(r) && MatchesDay(r) &&
            string.Equals(r.TimeSlot?.Trim(), legacyTimeSlot, StringComparison.OrdinalIgnoreCase));

        matchedRule ??= rules.FirstOrDefault(r =>
            MatchesBase(r) && MatchesDay(r) &&
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