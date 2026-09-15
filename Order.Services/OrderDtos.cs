namespace Order.Services;

public class OrderItemResponse
{
    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }
}

public class OrderResponse
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public decimal Total { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public List<OrderItemResponse> Items { get; set; }
        = new List<OrderItemResponse>();
}