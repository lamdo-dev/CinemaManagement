namespace CinemaManagement.Models;

public class PricingRule
{
    public int Id { get; set; }

    public string Format { get; set; } = "2D";

    public string SeatType { get; set; } = "Ghế thường";

    public string DayType { get; set; } = "Ngày thường";

    public string TimeSlot { get; set; } = "Cả ngày";

    public decimal BasePrice { get; set; }

    public decimal Surcharge { get; set; }

    public decimal FinalPrice => BasePrice + Surcharge;
}