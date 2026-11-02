namespace CinemaManagement.Models;

public class Room
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // Ví dụ: "Phòng 01", "Phòng IMAX"
    public int TotalSeats { get; set; }
    public int TotalRows { get; set; }
    public int TotalColumns { get; set; }
}