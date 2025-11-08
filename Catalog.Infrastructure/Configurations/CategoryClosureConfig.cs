using Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Configurations;

public class CategoryClosureConfig : IEntityTypeConfiguration<CategoryClosure>
{
    public void Configure(EntityTypeBuilder<CategoryClosure> b)
    {
        b.ToTable("CategoryClosure");
        b.HasKey(x => new { x.AncestorId, x.DescendantId });

        b.Property(x => x.Depth).IsRequired();

        b.HasIndex(x => x.DescendantId);

        b.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.AncestorId)
            .OnDelete(DeleteBehavior.NoAction);

        b.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.DescendantId)
            .OnDelete(DeleteBehavior.NoAction);

    }
}