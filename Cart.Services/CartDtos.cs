namespace Cart.Services;

public class AddToCartRequest
{
    public Guid ProductId { get; set; }

    public int Quantity { get; set; }
}

public class ProductInCartResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Quantity { get; set; }
}

public class CartResponse
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public List<ProductInCartResponse> Products { get; set; }
        = new List<ProductInCartResponse>();
}