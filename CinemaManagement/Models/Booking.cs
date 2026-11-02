namespace CinemaManagement.Models;

public class Booking
{
    public int Id { get; set; }
    public int UserId { get; set; } // Người tạo đơn (Khách hàng hoặc Thu ngân)
    public string BookingCode { get; set; } = string.Empty; // Mã giao dịch
    public DateTime BookingTime { get; set; } = DateTime.Now;
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
    public decimal FinalAmount { get; set; }
    public string PaymentMethod { get; set; } = "Cash"; // "Cash", "Transfer", "Momo"
}