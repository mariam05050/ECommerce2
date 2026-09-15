using AutoMapper;
using Product.Data;

namespace Product.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly IMapper _mapper;

    public ProductService(
        IProductRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<List<ProductResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var products = await _repository.GetAllAsync(cancellationToken);

        return _mapper.Map<List<ProductResponse>>(products);
    }

    public async Task<ProductResponse> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var product = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (product is null)
        {
            throw new KeyNotFoundException(
                $"Product with ID '{id}' was not found.");
        }

        return _mapper.Map<ProductResponse>(product);
    }

    public async Task<ProductResponse> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var exists = await _repository.ExistsByNameAsync(
            request.Name,
            cancellationToken: cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(
                "A product with the same name already exists.");
        }

        var product = _mapper.Map<Product.Data.Product>(request);

        product.Id = Guid.NewGuid();
        product.CreatedAtUtc = DateTime.UtcNow;
        product.IsDeleted = false;

        _repository.Add(product);

        await _repository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ProductResponse>(product);
    }

    public async Task<ProductResponse> UpdateAsync(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await _repository.GetByIdForUpdateAsync(
            id,
            cancellationToken);

        if (product is null)
        {
            throw new KeyNotFoundException(
                $"Product with ID '{id}' was not found.");
        }

        var exists = await _repository.ExistsByNameAsync(
            request.Name,
            id,
            cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(
                "A product with the same name already exists.");
        }

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.Stock = request.Stock;
        product.UpdatedAtUtc = DateTime.UtcNow;

        _repository.Update(product);

        await _repository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ProductResponse>(product);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var product = await _repository.GetByIdForUpdateAsync(
            id,
            cancellationToken);

        if (product is null)
        {
            throw new KeyNotFoundException(
                $"Product with ID '{id}' was not found.");
        }

        product.IsDeleted = true;
        product.UpdatedAtUtc = DateTime.UtcNow;

        _repository.Update(product);

        await _repository.SaveChangesAsync(cancellationToken);
    }
}