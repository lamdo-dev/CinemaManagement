namespace CinemaManagement.Models;

/// <summary>
/// Đại diện cho một sản phẩm bắp nước/combo trong giỏ hàng đặt vé.
/// </summary>
public readonly record struct CartItem(
    string Name,
    decimal Price,
    int Qty,
    string ImgPath = "")
{
    public decimal TotalPrice => Price * Qty;
}
