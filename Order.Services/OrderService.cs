using Microsoft.EntityFrameworkCore.Storage;
using Order.Data;
using Tracking.Data;
using Tracking.Services;

namespace Order.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;
    private readonly ITrackingService _trackingService;

    public OrderService(
        IOrderRepository repository,
        ITrackingService trackingService)
    {
        _repository = repository;
        _trackingService = trackingService;
    }

    public async Task<OrderResponse> CheckoutAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Start one database transaction for the checkout.
        var transaction = await _repository.BeginTransactionAsync(
            cancellationToken);

        try
        {
            // Get the user's cart with products and quantities.
            var cart = await _repository.GetCartByUserIdAsync(
                userId,
                cancellationToken);

            if (cart is null)
            {
                throw new KeyNotFoundException(
                    "Cart was not found.");
            }

            if (!cart.CartProducts.Any())
            {
                throw new InvalidOperationException(
                    "Cart is empty.");
            }

            decimal total = 0;

            // Validate products, quantities, and stock.
            foreach (var cartProduct in cart.CartProducts)
            {
                var product = cartProduct.Product;

                if (product is null || product.IsDeleted)
                {
                    throw new KeyNotFoundException(
                        "A product in the cart was not found.");
                }

                if (cartProduct.Quantity <= 0)
                {
                    throw new InvalidOperationException(
                        "Cart contains an invalid quantity.");
                }

                if (cartProduct.Quantity > product.Stock)
                {
                    throw new InvalidOperationException(
                        $"Not enough stock for product '{product.Name}'.");
                }

                total += product.Price * cartProduct.Quantity;
            }

            // Create the order.
            var order = new Order.Data.Order
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Total = total,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = null,
                IsDeleted = false
            };

            _repository.AddOrder(order);

            // Convert cart items into order items.
            foreach (var cartProduct in cart.CartProducts)
            {
                var product = cartProduct.Product;

                var orderProduct = new OrderProduct
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = product.Id,
                    Quantity = cartProduct.Quantity,
                    UnitPrice = product.Price
                };

                // Reduce stock.
                product.Stock -= cartProduct.Quantity;

                _repository.AddOrderProduct(orderProduct);

                // Remove purchased item from cart.
                _repository.RemoveCartProduct(cartProduct);
            }

            // Save order, order items, stock changes,
            // and cart changes.
            await _repository.SaveChangesAsync(
                cancellationToken);

            // Add the initial tracking status.
            await _trackingService.UpdateStatusAsync(
                order.Id,
                TrackingStatus.Processing,
                cancellationToken);

            // Everything succeeded, so commit the transaction.
            await _repository.CommitTransactionAsync(
                transaction,
                cancellationToken);

            return await BuildOrderResponseAsync(
                order,
                cancellationToken);
        }
        catch
        {
            // Something failed, so roll back everything.
            await _repository.RollbackTransactionAsync(
                transaction,
                cancellationToken);

            throw;
        }
    }

    public async Task<List<OrderResponse>> GetMyOrdersAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var orders = await _repository.GetOrdersByUserIdAsync(
            userId,
            cancellationToken);

        var responses = new List<OrderResponse>();

        foreach (var order in orders)
        {
            responses.Add(
                await BuildOrderResponseAsync(
                    order,
                    cancellationToken));
        }

        return responses;
    }

    public async Task<OrderResponse> GetByIdAsync(
        Guid userId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _repository.GetByIdAsync(
            orderId,
            cancellationToken);

        if (order is null || order.UserId != userId)
        {
            throw new KeyNotFoundException(
                "Order was not found.");
        }

        return await BuildOrderResponseAsync(
            order,
            cancellationToken);
    }

    private async Task<OrderResponse> BuildOrderResponseAsync(
        Order.Data.Order order,
        CancellationToken cancellationToken)
    {
        var orderProducts =
            await _repository.GetOrderProductsByOrderIdAsync(
                order.Id,
                cancellationToken);

        var items = new List<OrderItemResponse>();

        foreach (var orderProduct in orderProducts)
        {
            var product = await _repository.GetProductByIdAsync(
                orderProduct.ProductId,
                cancellationToken);

            items.Add(new OrderItemResponse
            {
                ProductId = orderProduct.ProductId,
                ProductName = product?.Name ?? "Unknown Product",
                Quantity = orderProduct.Quantity,
                UnitPrice = orderProduct.UnitPrice,
                TotalPrice =
                    orderProduct.UnitPrice *
                    orderProduct.Quantity
            });
        }

        return new OrderResponse
        {
            Id = order.Id,
            UserId = order.UserId,
            Total = order.Total,
            CreatedAtUtc = order.CreatedAtUtc,
            Items = items
        };
    }
}