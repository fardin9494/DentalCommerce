using BuildingBlocks.Domain;

namespace Catalog.Domain.Categories;


    public enum CategoryStatus { Draft = 0, Active = 1, Hidden = 2 }

    public sealed class Category : AggregateRoot<Guid>
    {
        public string Name { get; private set; } = null!;
        public string DefaultSlug { get; private set; } = null!;
        public Guid? ParentId { get; private set; }
        public int SortOrder { get; private set; }
        public CategoryStatus Status { get; private set; } = CategoryStatus.Active;
        public string? Icon { get; private set; }
        public string? ContentBlocksJson { get; private set; }

        private Category() { } 

        public static Category Create(string name, string defaultSlug, Guid? parentId = null, int sortOrder = 0)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("نام اجباری است");
            if (string.IsNullOrWhiteSpace(defaultSlug)) throw new ArgumentException("اسلاگ اجباری است");

            return new Category
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                DefaultSlug = defaultSlug.Trim().ToLowerInvariant(),
                ParentId = parentId,
                SortOrder = sortOrder
            };
        }

        public void Rename(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.");
            Name = name.Trim();
            Touch();
        }
        public void SetSlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug is required.");
            DefaultSlug = slug.Trim().ToLowerInvariant();
            Touch();
        }
        public void SetParent(Guid? newParentId)
        {
            // تغییر نکند اگر همانه
            if (ParentId == newParentId) return;

            // گارد: والدِ خودش نباشد
            if (newParentId.HasValue && newParentId.Value == Id)
                throw new InvalidOperationException("Category cannot be its own parent.");

            ParentId = newParentId;
            Touch();
        }

    public void MoveTo(Guid? newParentId)
        {
            ParentId = newParentId;
            Touch();
        }

        public void Reorder(int sortOrder)
        {
            SortOrder = sortOrder;
            Touch();
        }

        public void SetContent(string? icon, string? contentBlocksJson)
        {
            Icon = icon;
            ContentBlocksJson = contentBlocksJson;
            Touch();
        }

        public void SetStatus(CategoryStatus status)
        {
            Status = status;
            Touch();
        }

    
}
