using Product.Data;

namespace Cart.Data;

public class CartProduct
{
    public Guid CartId { get; set; }

    public Guid ProductId { get; set; }

    public int Quantity { get; set; }

    public Product.Data.Product Product { get; set; } = null!;
}