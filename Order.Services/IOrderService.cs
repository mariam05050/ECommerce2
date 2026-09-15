namespace Order.Services;

public interface IOrderService
{
    Task<OrderResponse> CheckoutAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<List<OrderResponse>> GetMyOrdersAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<OrderResponse> GetByIdAsync(
        Guid userId,
        Guid orderId,
        CancellationToken cancellationToken = default);
}