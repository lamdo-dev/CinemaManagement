namespace CinemaManagement.Models;

using CinemaManagement.Models.Enums;

public class Seat
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string SeatNumber { get; set; } = string.Empty; // Ví dụ: "A1", "B5"
    public int RowIndex { get; set; }
    public int ColumnIndex { get; set; }
    public SeatType Type { get; set; } = SeatType.Standard;
    public decimal PriceMultiplier { get; set; } = 1.0m; // Hệ số giá (Ghế VIP = 1.2, Ghế đôi = 2.0)
}