namespace Cart.Data;

public class Cart
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public ICollection<CartProduct> CartProducts { get; set; }
        = new List<CartProduct>();
}