namespace Product.Data;

public interface IProductRepository
{
    Task<List<Product>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Product?> GetByIdForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    void Add(Product product);

    void Update(Product product);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}