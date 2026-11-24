namespace CinemaManagement.Models;

public class Promotion
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string DiscountType { get; set; } = "Phần trăm";

    public decimal DiscountValue { get; set; }

    public decimal MaxDiscount { get; set; }

    public decimal MinimumOrder { get; set; }

    public int UsageLimit { get; set; } = 100;

    public int UsedCount { get; set; }

    public int RemainingUses => Math.Max(0, UsageLimit - UsedCount);

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsEnabled { get; set; } = true;
}