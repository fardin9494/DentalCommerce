namespace Catalog.Api.Permissions;

public static class CatalogPermissionKeys
{
    public const string ProductsView = "Catalog.Products.View";
    public const string ProductsCreate = "Catalog.Products.Create";
    public const string ProductsBasicsEdit = "Catalog.Products.Basics.Edit";
    public const string ProductsDescriptionEdit = "Catalog.Products.Description.Edit";
    public const string ProductsSeoEdit = "Catalog.Products.Seo.Edit";
    public const string ProductsActivate = "Catalog.Products.Activate";
    public const string ProductsHide = "Catalog.Products.Hide";
    public const string ProductsResolveBySku = "Catalog.Products.ResolveBySku";
    public const string ProductsValidate = "Catalog.Products.Validate";

    public const string ProductsCategoriesView = "Catalog.Products.Categories.View";
    public const string ProductsCategoriesEdit = "Catalog.Products.Categories.Edit";
    public const string ProductsCategoriesPrimaryEdit = "Catalog.Products.Categories.Primary.Edit";

    public const string ProductsStoresView = "Catalog.Products.Stores.View";
    public const string ProductsStoresManage = "Catalog.Products.Stores.Manage";

    public const string ProductsImagesUpload = "Catalog.Products.Images.Upload";
    public const string ProductsImagesSetMain = "Catalog.Products.Images.SetMain";
    public const string ProductsImagesReorder = "Catalog.Products.Images.Reorder";
    public const string ProductsImagesDelete = "Catalog.Products.Images.Delete";

    public const string ProductsVariationEdit = "Catalog.Products.Variation.Edit";
    public const string ProductsVariantsView = "Catalog.Products.Variants.View";
    public const string ProductsVariantsCreate = "Catalog.Products.Variants.Create";
    public const string ProductsVariantsEdit = "Catalog.Products.Variants.Edit";
    public const string ProductsVariantsDelete = "Catalog.Products.Variants.Delete";

    public const string ProductsPropertiesView = "Catalog.Products.Properties.View";
    public const string ProductsPropertiesCreate = "Catalog.Products.Properties.Create";
    public const string ProductsPropertiesDelete = "Catalog.Products.Properties.Delete";

    public const string CategoriesTreeView = "Catalog.Categories.Tree.View";
    public const string CategoriesLeavesView = "Catalog.Categories.Leaves.View";
    public const string CategoriesLeavesWithProductsView = "Catalog.Categories.LeavesWithProducts.View";
    public const string CategoriesFlagsView = "Catalog.Categories.Flags.View";
    public const string CategoriesCreate = "Catalog.Categories.Create";
    public const string CategoriesRename = "Catalog.Categories.Rename";
    public const string CategoriesMove = "Catalog.Categories.Move";

    public const string BrandsView = "Catalog.Brands.View";
    public const string BrandsDetailView = "Catalog.Brands.Detail.View";
    public const string BrandsCreate = "Catalog.Brands.Create";
    public const string BrandsEdit = "Catalog.Brands.Edit";
    public const string BrandsRename = "Catalog.Brands.Rename";
    public const string BrandsProfileEdit = "Catalog.Brands.Profile.Edit";
    public const string BrandsStatusEdit = "Catalog.Brands.Status.Edit";
    public const string BrandsLogoUpload = "Catalog.Brands.Logo.Upload";
    public const string BrandsDelete = "Catalog.Brands.Delete";
    public const string BrandsAliasesView = "Catalog.Brands.Aliases.View";
    public const string BrandsAliasesManage = "Catalog.Brands.Aliases.Manage";

    public const string StoresView = "Catalog.Stores.View";
    public const string StoresDetailView = "Catalog.Stores.Detail.View";
    public const string StoresCreate = "Catalog.Stores.Create";
    public const string StoresRename = "Catalog.Stores.Rename";
    public const string StoresDomainEdit = "Catalog.Stores.Domain.Edit";

    public const string CountriesView = "Catalog.Countries.View";
    public const string CountriesCreate = "Catalog.Countries.Create";
    public const string CountriesEdit = "Catalog.Countries.Edit";
    public const string CountriesDelete = "Catalog.Countries.Delete";

    public const string MetadataPropertyKeysView = "Catalog.Metadata.PropertyKeys.View";
    public const string MetadataPropertyValuesView = "Catalog.Metadata.PropertyValues.View";
    public const string MetadataVariantValuesView = "Catalog.Metadata.VariantValues.View";
    public const string MetadataVariantRecentView = "Catalog.Metadata.VariantRecent.View";
}
