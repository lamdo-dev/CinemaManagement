using System;

namespace CinemaManagement.Models;

public class StaffTicketScanLog
{
    public long Id { get; set; }

    public string BookingCode { get; set; } = string.Empty;

    public string ScanStatus { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime ScannedAt { get; set; }

    public int? ShowtimeId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string MovieTitle { get; set; } = string.Empty;

    public string ShowtimeStartTime { get; set; } = string.Empty;

    public string SeatNumbers { get; set; } = string.Empty;

    public string StaffName { get; set; } = string.Empty;
}