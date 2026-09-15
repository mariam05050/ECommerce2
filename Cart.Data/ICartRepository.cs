namespace Cart.Data;

public interface ICartRepository
{
    Task<Cart?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Cart?> GetByUserIdWithProductsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Product.Data.Product?> GetProductByIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    void Add(Cart cart);

    void AddCartProduct(CartProduct cartProduct);

    void RemoveCartProduct(CartProduct cartProduct);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}