namespace Order.Data;

public interface IOrderRepository
{
    Task<Cart.Data.Cart?> GetCartByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<List<Order>> GetOrdersByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Order?> GetByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<List<OrderProduct>> GetOrderProductsByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<Product.Data.Product?> GetProductByIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    void AddOrder(Order order);

    void AddOrderProduct(OrderProduct orderProduct);

    void RemoveCartProduct(
        Cart.Data.CartProduct cartProduct);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}