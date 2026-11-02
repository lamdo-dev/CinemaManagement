namespace CinemaManagement.Models;

public class Showtime
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public int RoomId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal BasePrice { get; set; } // Giá vé gốc cho suất chiếu này
}