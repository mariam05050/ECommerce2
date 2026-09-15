using Order.Data;

namespace Order.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;

    public OrderService(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<OrderResponse> CheckoutAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Get the user's cart with its products and quantities.
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

        // Validate stock and calculate the order total.
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


        // Convert every cart item into an order item.
        foreach (var cartProduct in cart.CartProducts)
        {
            var product = cartProduct.Product;

            var orderProduct = new OrderProduct
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = product.Id,
                Quantity = cartProduct.Quantity,

                // Save the price at checkout time.
                UnitPrice = product.Price
            };

            // Reduce inventory only after checkout is being created.
            product.Stock -= cartProduct.Quantity;

            _repository.AddOrderProduct(orderProduct);

            // Remove purchased item from the cart.
            _repository.RemoveCartProduct(cartProduct);
        }

        await _repository.SaveChangesAsync(
            cancellationToken);

        return await BuildOrderResponseAsync(
            order,
            cancellationToken);
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