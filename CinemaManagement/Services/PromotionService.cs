using CinemaManagement.Models;
using CinemaManagement.Repositories;

namespace CinemaManagement.Services;

public class PromotionService
{
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

        if (promotion.DiscountType == "Phần trăm" &&
            promotion.DiscountValue > 100)
        {
            throw new ArgumentException(
                "Mức giảm phần trăm không được vượt quá 100%."
            );
        }

        if (promotion.MaxDiscount < 0)
            throw new ArgumentException("Mức giảm tối đa không hợp lệ.");

        if (promotion.MinimumOrder < 0)
            throw new ArgumentException("Đơn hàng tối thiểu không hợp lệ.");

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

        if (_repository.GetById(promotion.Id) == null)
        {
            throw new InvalidOperationException(
                "Không tìm thấy chương trình cần cập nhật."
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
}