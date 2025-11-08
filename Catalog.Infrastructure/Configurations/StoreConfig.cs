using Catalog.Domain.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Configurations;

public class StoreConfig : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> b)
    {
        b.ToTable("Store");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).IsRequired().HasMaxLength(128);
        b.Property(x => x.Domain).HasMaxLength(256);

        b.HasIndex(x => x.Name);
        b.HasIndex(x => x.Domain)
            .IsUnique()
            .HasFilter("[Domain] IS NOT NULL"); // SQL Server

    }
}