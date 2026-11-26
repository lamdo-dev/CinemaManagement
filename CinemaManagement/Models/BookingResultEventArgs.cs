using System;
using System.Collections.Generic;

namespace CinemaManagement.Models;

/// <summary>
/// Dữ liệu kết quả đơn đặt vé xuyên suốt các tầng UI, Service và Repository.
/// </summary>
public class BookingResultEventArgs : EventArgs
{
    public int? UserId { get; set; }

    public string BookingCode { get; set; } = string.Empty;

    public Movie? Movie { get; set; }

    public Showtime? Showtime { get; set; }

    public List<string> Seats { get; set; } = new();

    public Dictionary<int, CartItem> FoodCart { get; set; } = new();

    // Tổng tiền vé trước khi áp dụng khuyến mãi.
    public decimal TicketAmount { get; set; }

    // Tổng tiền bắp nước.
    public decimal FoodAmount { get; set; }

    // Tổng tiền khách phải thanh toán sau khi trừ khuyến mãi.
    public decimal TotalAmount { get; set; }

    public string PromotionCode { get; set; } = string.Empty;

    public decimal DiscountAmount { get; set; }

    public DateTime BookingTime { get; set; }

    // Online hoặc POS
    public string Channel { get; set; } = "Online";

    public string PaymentMethod { get; set; } = "Thanh toán mô phỏng";

    public string PaymentStatus { get; set; } = "PAID";

    public string CustomerName { get; set; } = string.Empty;

    public int EarnedPoints { get; set; }
}
