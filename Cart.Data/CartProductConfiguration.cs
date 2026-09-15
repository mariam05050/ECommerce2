using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cart.Data;

public class CartProductConfiguration
    : IEntityTypeConfiguration<CartProduct>
{
    public void Configure(
        EntityTypeBuilder<CartProduct> builder)
    {
        builder.ToTable("CartProducts");

        builder.HasKey(x => new
        {
            x.CartId,
            x.ProductId
        });

        builder.Property(x => x.Quantity)
            .IsRequired();

        builder.ToTable(
            "CartProducts",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_CartProducts_Quantity_Positive",
                    "[Quantity] > 0");
            });

        builder.HasOne<Cart>()
            .WithMany(x => x.CartProducts)
            .HasForeignKey(x => x.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}