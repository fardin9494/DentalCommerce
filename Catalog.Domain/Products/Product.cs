using BuildingBlocks.Domain;

namespace Catalog.Domain.Products;

public enum ProductStatus { Draft = 0, Active = 1, Hidden = 2 }
public enum VariationMode { None = 0, SingleAttribute = 1 } 

public sealed class Product : AggregateRoot<Guid>
{
    public string Name { get; private set; } = null!;
    public string DefaultSlug { get; private set; } = null!;
    public string Code { get; private set; } = null!;         
    public string? WarehouseCode { get; private set; }        
    public Guid BrandId { get; private set; }
    public Guid? PrimaryCategoryId { get; private set; }
    public ProductStatus Status { get; private set; } = ProductStatus.Draft;


    public VariationMode VariationMode { get; private set; } = VariationMode.None;
    public string? VariationKey { get; private set; }           


    public Guid? MainImageId { get; private set; }    
    
    private readonly List<ProductVariant> _variants = new();
    private readonly List<ProductProperty> _properties = new();
    private readonly List<ProductImage> _images = new();
    private readonly List<ProductStore> _stores = new();
    private readonly List<ProductSeo> _seos = new();
    private readonly List<ProductCategory> _categories = new(); 
    public IReadOnlyCollection<ProductCategory> Categories => _categories;
    public IReadOnlyCollection<ProductImage> Images => _images;
    public IReadOnlyCollection<ProductProperty> Properties => _properties;
    public IReadOnlyCollection<ProductVariant> Variants => _variants;
    public IReadOnlyCollection<ProductStore> Stores => _stores;
    public IReadOnlyCollection<ProductSeo> Seos => _seos;

    private Product() { }

    public static Product Create(
        string name, string defaultSlug, string code,
        Guid brandId,
        string? warehouseCode = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required");
        if (string.IsNullOrWhiteSpace(defaultSlug)) throw new ArgumentException("Slug required");
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code required");

        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            DefaultSlug = defaultSlug.Trim().ToLowerInvariant(),
            Code = code.Trim().ToUpperInvariant(),
            WarehouseCode = string.IsNullOrWhiteSpace(warehouseCode) ? null : warehouseCode.Trim(),
            BrandId = brandId,
          
        };
    }

    public void SetVariation(string? variationKey)
    {
        if (string.IsNullOrWhiteSpace(variationKey))
        {
            VariationMode = VariationMode.None;
            VariationKey = null;
            _variants.Clear();
        }
        else
        {
            VariationMode = VariationMode.SingleAttribute;
            VariationKey = variationKey.Trim();
        }
        Touch();
    }

    public ProductVariant AddOrUpdateVariant(string variantValue, string sku, bool isActive = true)
    {
        if (VariationMode != VariationMode.SingleAttribute)
            throw new InvalidOperationException("VariationMode must be SingleAttribute to add variants.");

        var v = _variants.FirstOrDefault(x => x.Value == variantValue);
        if (v is null)
        {
            v = ProductVariant.Create(Id, variantValue.Trim(), sku.Trim(), isActive);
            _variants.Add(v);
        }
        else
        {
            v.UpdateSku(sku.Trim());
            v.SetActive(isActive);
        }
        Touch();
        return v;
    }

    public void SetWarehouseCode(string? code)
    {
        WarehouseCode = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        Touch();
    }

    public void Rename(string name)
    {
        Name = name.Trim();
        Touch();
    }
    public void AddCategory(Guid categoryId, bool makePrimary = false)
    {
        if (_categories.Any(c => c.CategoryId == categoryId))
            return;

        _categories.Add(ProductCategory.Link(Id, categoryId, isPrimary: makePrimary));
        if (makePrimary || PrimaryCategoryId is null)
            PrimaryCategoryId = categoryId;

        Touch();
    }

    public void RemoveCategory(Guid categoryId)
    {
        var link = _categories.FirstOrDefault(c => c.CategoryId == categoryId);
        if (link is null) return;

        _categories.Remove(link);

        if (PrimaryCategoryId == categoryId)
            PrimaryCategoryId = _categories.FirstOrDefault(c => c.IsPrimary)?.CategoryId
                                ?? _categories.FirstOrDefault()?.CategoryId;

        Touch();
    }

    public void SetPrimaryCategory(Guid categoryId)
    {
        if (_categories.All(c => c.CategoryId != categoryId))
            throw new InvalidOperationException("Category not linked to product.");

        foreach (var c in _categories) c.SetPrimary(c.CategoryId == categoryId);
        PrimaryCategoryId = categoryId;
        Touch();
    }

    public void SetStatus(ProductStatus status)
    {
        Status = status;
        Touch();
    }

    public void UpsertProperty(string key, string? s = null, decimal? d = null, bool? b = null, string? j = null)
    {
        var k = key.Trim();
        var p = _properties.FirstOrDefault(x => x.Key == k);
        if (p is null)
        {
            _properties.Add(ProductProperty.Create(Id, k, s, d, b, j));
        }
        else
        {
            p.Set(s, d, b, j);
        }
        Touch();
    }

    public ProductImage AddImage(string url, string? alt = null, int sortOrder = 0, bool makeMain = false)
    {
        var img = ProductImage.Create(Id, url, alt, sortOrder);
        _images.Add(img);
        Touch();
        return img;
    }

    public void SetMainImage(Guid imageId)
    {
        if (_images.All(i => i.Id != imageId))
            throw new InvalidOperationException("Image not found in album.");
        MainImageId = imageId;
        Touch();
    }

    public void UpsertStore(Guid storeId, bool isVisible, string slug, string? titleOverride = null, string? descriptionOverride = null)
    {
        var s = _stores.FirstOrDefault(x => x.StoreId == storeId);
        if (s is null)
            _stores.Add(ProductStore.Create(Id, storeId, isVisible, slug, titleOverride, descriptionOverride));
        else
            s.Update(isVisible, slug, titleOverride, descriptionOverride);

        Touch();
    }

    public void UpsertSeo(Guid storeId, string? metaTitle, string? metaDescription, string? canonicalUrl, string? robots, string? jsonLd)
    {
        var seo = _seos.FirstOrDefault(x => x.StoreId == storeId);
        if (seo is null)
            _seos.Add(ProductSeo.Create(Id, storeId, metaTitle, metaDescription, canonicalUrl, robots, jsonLd));
        else
            seo.Update(metaTitle, metaDescription, canonicalUrl, robots, jsonLd);

        Touch();
    }

  
}
