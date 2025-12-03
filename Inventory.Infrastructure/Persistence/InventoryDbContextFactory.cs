using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Inventory.Infrastructure.Persistence;

public sealed class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>();
        var cs = "Server=DESKTOP-RAJN9B2\\FARDIN2019;Database=DentalCatalogDb;Integrated Security=True;TrustServerCertificate=True";
        options.UseSqlServer(cs, sql =>
        {
            sql.MigrationsHistoryTable("__EFMigrationsHistory", InventoryDbContext.DefaultSchema);
        });
        return new InventoryDbContext(options.Options);
    }
}