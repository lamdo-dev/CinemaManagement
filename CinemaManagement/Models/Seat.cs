using CinemaManagement.Models.Enums;

namespace CinemaManagement.Models
{
    public class Seat
    {
        public int Id { get; set; }

        public int RoomId { get; set; }

        // Ví dụ: A1, B5
        public string SeatNumber { get; set; } = string.Empty;

        public int RowIndex { get; set; }

        public int ColumnIndex { get; set; }

        public SeatType Type { get; set; } = SeatType.Standard;

        // Hệ số giá: Standard = 1.0, VIP = 1.2, Couple = 2.0
        public double PriceMultiplier { get; set; } = 1.0;
    }
}