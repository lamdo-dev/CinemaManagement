namespace CinemaManagement.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // Ví dụ: "Bắp Bơ Lớn", "Coca-Cola"
    public string Category { get; set; } = string.Empty; // "Food", "Drink", "Combo"
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}