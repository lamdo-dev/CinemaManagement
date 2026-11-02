namespace CinemaManagement.Models;

public class BookingDetailProduct
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}