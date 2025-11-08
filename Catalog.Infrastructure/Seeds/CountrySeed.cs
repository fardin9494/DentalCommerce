using Catalog.Domain.Brands;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Seeds;

public static class CountrySeed
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Country>().HasData(
            Country.Create("IR", "IRN", "ایران", "Iran", "Asia", "🇮🇷"),
            Country.Create("TR", "TUR", "ترکیه", "Türkiye", "Asia", "🇹🇷"),
            Country.Create("DE", "DEU", "آلمان", "Germany", "Europe", "🇩🇪"),
            Country.Create("US", "USA", "آمریکا", "United States", "Americas", "🇺🇸"),
            Country.Create("CN", "CHN", "چین", "China", "Asia", "🇨🇳"),
            Country.Create("JP", "JPN", "ژاپن", "Japan", "Asia", "🇯🇵"),
            Country.Create("IT", "ITA", "ایتالیا", "Italy", "Europe", "🇮🇹")
        );
    }
}