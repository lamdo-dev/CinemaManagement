using System;

namespace CinemaManagement.Models;

public class Showtime
{
    public int Id { get; set; }

    public int MovieId { get; set; }

    public int RoomId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    // Giá vé gốc của suất chiếu
    public decimal BasePrice { get; set; }

    // Định dạng phim: 2D hoặc 3D
    public string Format { get; set; } = "2D";
}