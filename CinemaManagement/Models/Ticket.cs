namespace CinemaManagement.Models;

using CinemaManagement.Models.Enums;

public class Ticket
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int ShowtimeId { get; set; }
    public int SeatId { get; set; }
    public string TicketCode { get; set; } = string.Empty; // Mã vé dùng để tạo QR Code
    public decimal Price { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Unused;
}