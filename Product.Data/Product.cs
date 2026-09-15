namespace Product.Data;

public class Product
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public DateTime CreatedAtUtc { get; set; }//idk why we need this

    public DateTime? UpdatedAtUtc { get; set; }//idk why we need this

    public bool IsDeleted { get; set; }
}