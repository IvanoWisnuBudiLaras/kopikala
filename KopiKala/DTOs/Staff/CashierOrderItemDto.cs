namespace KopiKala.DTOs.Staff;

public class CashierOrderItemDto
{
    public Guid DetailId { get; set; }
    public Guid MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
    public string OrderType { get; set; } = "PreOrder"; // "PreOrder" or "AddOn"
}
