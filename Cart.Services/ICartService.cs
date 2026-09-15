namespace Cart.Services;

public interface ICartService
{
    Task<CartResponse> GetMyCartAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<CartResponse> AddProductAsync(
        Guid userId,
        AddToCartRequest request,
        CancellationToken cancellationToken = default);

    Task RemoveProductAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default);

    Task ClearCartAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}