namespace Order.Data;

public class Order
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public decimal Total { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }
}