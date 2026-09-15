using Microsoft.EntityFrameworkCore;

namespace Order.Data;

public class OrderRepository : IOrderRepository
{
    private readonly IOrderDbContext _context;

    public OrderRepository(IOrderDbContext context)
    {
        _context = context;
    }

    public async Task<Cart.Data.Cart?> GetCartByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Carts
            .Include(x => x.CartProducts)
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(
                x => x.UserId == userId,
                cancellationToken);
    }

    public async Task<List<Order>> GetOrdersByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Where(x => x.UserId == userId)
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .FirstOrDefaultAsync(
                x => x.Id == orderId,
                cancellationToken);
    }

    public async Task<List<OrderProduct>> GetOrderProductsByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await _context.OrderProducts
            .Where(x => x.OrderId == orderId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Product.Data.Product?> GetProductByIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == productId,
                cancellationToken);
    }

    public void AddOrder(Order order)
    {
        _context.Orders.Add(order);
    }

    public void AddOrderProduct(OrderProduct orderProduct)
    {
        _context.OrderProducts.Add(orderProduct);
    }


    public void RemoveCartProduct(
        Cart.Data.CartProduct cartProduct)
    {
        _context.CartProducts.Remove(cartProduct);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}