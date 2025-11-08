using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Catalog.Infrastructure;

public class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();

      
        var connectionString =
            "Server=DESKTOP-RAJN9B2\\FARDIN2019;Database=DentalCatalogDb;Integrated Security=True;TrustServerCertificate=True";
        optionsBuilder.UseSqlServer(connectionString, sql =>
        {
            sql.MigrationsHistoryTable("__EFMigrationsHistory", CatalogDbContext.DefaultSchema);
        });

        return new CatalogDbContext(optionsBuilder.Options);
    }
}