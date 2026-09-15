using Microsoft.EntityFrameworkCore;

namespace Cart.Data;

public class CartRepository : ICartRepository
{
    private readonly ICartDbContext _context;

    public CartRepository(ICartDbContext context)
    {
        _context = context;
    }

    public async Task<Cart?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Carts
            .FirstOrDefaultAsync(
                x => x.UserId == userId,
                cancellationToken);
    }

    public async Task<Cart?> GetByUserIdWithProductsAsync(
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

    public void Add(Cart cart)
    {
        _context.Carts.Add(cart);
    }

    public void AddCartProduct(CartProduct cartProduct)
    {
        _context.CartProducts.Add(cartProduct);
    }

    public void RemoveCartProduct(CartProduct cartProduct)
    {
        _context.CartProducts.Remove(cartProduct);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}