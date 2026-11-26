using CinemaManagement.Models;
using CinemaManagement.Repositories;

namespace CinemaManagement.Services;

public class PromotionService
{
    public const decimal MaxPercentDiscount = 70m;

    private readonly PromotionRepository _repository = new();

    public List<Promotion> GetAll()
    {
        return _repository.GetAll();
    }

    public Promotion? GetById(int id)
    {
        if (id <= 0)
            throw new ArgumentException("Mã khuyến mãi không hợp lệ.");

        return _repository.GetById(id);
    }

    private void Validate(Promotion promotion)
    {
        if (promotion == null)
            throw new ArgumentNullException(nameof(promotion));

        if (string.IsNullOrWhiteSpace(promotion.Code))
            throw new ArgumentException("Vui lòng nhập mã voucher.");

        if (string.IsNullOrWhiteSpace(promotion.Name))
            throw new ArgumentException("Vui lòng nhập tên chương trình.");

        promotion.Code = promotion.Code.Trim().ToUpperInvariant();
        promotion.Name = promotion.Name.Trim();

        if (promotion.DiscountType != "Phần trăm" &&
            promotion.DiscountType != "Số tiền")
        {
            throw new ArgumentException("Loại giảm giá không hợp lệ.");
        }

        if (promotion.DiscountValue <= 0)
            throw new ArgumentException("Giá trị giảm phải lớn hơn 0.");

        if (promotion.DiscountType == "Phần trăm")
        {
            if (promotion.DiscountValue > MaxPercentDiscount)
            {
                throw new ArgumentException(
                    $"Mức giảm phần trăm tối đa là {MaxPercentDiscount:0}% (không được nhập 100%)."
                );
            }

            if (promotion.MaxDiscount <= 0)
            {
                throw new ArgumentException(
                    "Loại giảm Phần trăm bắt buộc phải nhập Mức giảm tối đa lớn hơn 0 đ."
                );
            }
        }
        else if (promotion.DiscountType == "Số tiền")
        {
            if (promotion.MinimumOrder <= promotion.DiscountValue)
            {
                throw new ArgumentException(
                    "Với loại giảm Số tiền, Đơn hàng tối thiểu bắt buộc phải lớn hơn Giá trị giảm để tránh âm ngân sách."
                );
            }
        }

        if (promotion.MaxDiscount < 0)
            throw new ArgumentException("Mức giảm tối đa không hợp lệ.");

        if (promotion.MinimumOrder < 0)
            throw new ArgumentException("Đơn hàng tối thiểu không hợp lệ.");

        if (promotion.UsageLimit <= 0)
            throw new ArgumentException("Số lượt sử dụng tối đa (UsageLimit) phải lớn hơn 0.");

        if (promotion.EndDate.Date < promotion.StartDate.Date)
        {
            throw new ArgumentException(
                "Ngày kết thúc không được trước ngày bắt đầu."
            );
        }
    }

    public int Add(Promotion promotion)
    {
        Validate(promotion);

        if (_repository.ExistsByCode(promotion.Code))
        {
            throw new InvalidOperationException(
                "Mã voucher đã tồn tại."
            );
        }

        return _repository.Add(promotion);
    }

    public bool Update(Promotion promotion)
    {
        Validate(promotion);

        if (promotion.Id <= 0)
            throw new ArgumentException("Mã khuyến mãi không hợp lệ.");

        Promotion? existing = _repository.GetById(promotion.Id);
        if (existing == null)
        {
            throw new InvalidOperationException(
                "Không tìm thấy chương trình cần cập nhật."
            );
        }

        if (promotion.UsageLimit < existing.UsedCount)
        {
            throw new InvalidOperationException(
                $"Giới hạn lượt sử dụng ({promotion.UsageLimit}) không được nhỏ hơn số lượt đã dùng ({existing.UsedCount})."
            );
        }

        if (_repository.ExistsByCode(promotion.Code, promotion.Id))
        {
            throw new InvalidOperationException(
                "Mã voucher đã tồn tại."
            );
        }

        return _repository.Update(promotion);
    }

    public bool Delete(int id)
    {
        if (id <= 0)
            throw new ArgumentException("Mã khuyến mãi không hợp lệ.");

        if (_repository.GetById(id) == null)
        {
            throw new InvalidOperationException(
                "Không tìm thấy chương trình cần xóa."
            );
        }

        return _repository.Delete(id);
    }

    public int CountAll()
    {
        return _repository.CountAll();
    }

    public int CountActive()
    {
        return GetAll().Count(p =>
            p.IsEnabled &&
            (p.UsageLimit <= 0 || p.UsedCount < p.UsageLimit) &&
            p.StartDate.Date <= DateTime.Today &&
            p.EndDate.Date >= DateTime.Today
        );
    }

    public int CountUpcoming()
    {
        return GetAll().Count(p =>
            p.IsEnabled &&
            p.StartDate.Date > DateTime.Today
        );
    }

    /// <summary>
    /// Kiểm tra điều kiện áp dụng mã voucher, lượt dùng còn lại,
    /// giới hạn 1 lần/tài khoản và tính số tiền được giảm.
    /// </summary>
    public (Promotion Promotion, decimal DiscountAmount) ValidateAndCalculateDiscount(
        string code,
        decimal orderAmount,
        int? userId = null,
        IReadOnlyCollection<Promotion>? cachedPromotions = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("Vui lòng nhập mã voucher.");

        if (orderAmount <= 0)
            throw new InvalidOperationException("Tổng giá trị đơn hàng phải lớn hơn 0 đ.");

        string normalizedCode = code.Trim().ToUpperInvariant();
        IReadOnlyCollection<Promotion> promotions = cachedPromotions ?? GetAll();

        Promotion? promotion = promotions.FirstOrDefault(p =>
            string.Equals(
                p.Code?.Trim(),
                normalizedCode,
                StringComparison.OrdinalIgnoreCase));

        if (promotion == null)
            throw new InvalidOperationException("Mã voucher không tồn tại.");

        var result = CalculateDiscountForPromotion(promotion, orderAmount);

        if (userId.HasValue &&
            userId.Value > 0 &&
            _repository.HasUserUsedPromotion(userId.Value, promotion.Code))
        {
            throw new InvalidOperationException(
                "Tài khoản của bạn đã sử dụng mã voucher này trước đó (mỗi tài khoản chỉ được dùng 1 lần).");
        }

        return result;
    }

    public static (Promotion Promotion, decimal DiscountAmount) CalculateDiscountForPromotion(
        Promotion promotion,
        decimal orderAmount)
    {
        if (promotion == null)
            throw new InvalidOperationException("Mã voucher không tồn tại.");

        if (!promotion.IsEnabled)
            throw new InvalidOperationException("Voucher hiện đã bị vô hiệu hóa.");

        DateTime today = DateTime.Today;
        if (today < promotion.StartDate.Date || today > promotion.EndDate.Date)
        {
            throw new InvalidOperationException(
                "Voucher chưa đến thời gian áp dụng hoặc đã hết hạn.");
        }

        if (promotion.UsageLimit > 0 && promotion.UsedCount >= promotion.UsageLimit)
        {
            throw new InvalidOperationException("Voucher đã hết lượt sử dụng.");
        }

        if (orderAmount < promotion.MinimumOrder)
        {
            throw new InvalidOperationException(
                $"Đơn hàng cần đạt tối thiểu {promotion.MinimumOrder:N0} đ.");
        }

        decimal calculatedDiscount;
        if (promotion.DiscountType == "Phần trăm")
        {
            decimal safePercent = Math.Min(promotion.DiscountValue, MaxPercentDiscount);
            calculatedDiscount = Math.Round(
                orderAmount * safePercent / 100m,
                0);

            if (promotion.MaxDiscount > 0 &&
                calculatedDiscount > promotion.MaxDiscount)
            {
                calculatedDiscount = promotion.MaxDiscount;
            }
        }
        else if (promotion.DiscountType == "Số tiền")
        {
            if (orderAmount <= promotion.DiscountValue)
            {
                throw new InvalidOperationException(
                    $"Đơn hàng phải lớn hơn mức giảm {promotion.DiscountValue:N0} đ để áp dụng voucher này.");
            }

            calculatedDiscount = promotion.DiscountValue;
        }
        else
        {
            throw new InvalidOperationException(
                "Loại giảm giá của voucher không hợp lệ.");
        }

        // Chốt chặn cuối: không bao giờ cho phép giảm hết 100% giá trị đơn hàng
        if (calculatedDiscount >= orderAmount)
        {
            throw new InvalidOperationException(
                "Giá trị giảm của voucher vượt mức cho phép đối với đơn hàng này.");
        }

        if (calculatedDiscount <= 0)
        {
            throw new InvalidOperationException(
                "Voucher không tạo ra giá trị giảm hợp lệ.");
        }

        return (promotion, calculatedDiscount);
    }
}