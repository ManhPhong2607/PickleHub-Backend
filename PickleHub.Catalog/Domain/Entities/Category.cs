using PickleHub.Common.Domain;
using PickleHub.Common.Exceptions;
using PickleHub.Common.ValueObjects;
namespace PickleHub.Catalog.Domain.Entities
{
    public class Category : BaseEntity
    {      
        public string Name { get; private set; } = string.Empty;
        public Slug Slug { get; private set; } = null!;
        public Guid? ParentId { get; private set; }
        
        //fe dùng để render đúng field khi admin tạo variant sản phẩm thuộc category
        public string AttributeSchemaJson { get; private set; } = "[]";

        public virtual ICollection<Category> Children { get; private set; } = new List<Category>();

        private Category() { }

        public static Category Create(string name, Slug slug, Guid? parentId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Tên danh mục không được để trống.");

            return new Category
            {
                Name = name.Trim(),
                Slug = slug,
                ParentId = parentId
            };
        }

        public void Update(string name, Slug slug, Guid? parentId)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Tên danh mục không được để trống.");

            if (parentId.HasValue && parentId.Value == Id)
                throw new DomainException("Danh mục không thể là danh mục cha của chính nó.");
            Name = name;
            Slug = slug;
            ParentId = parentId;
            SetUpdated();
        }

        public void UpdateAttributeSchema(string attributeSchemaJson)
        {
            AttributeSchemaJson = string.IsNullOrWhiteSpace(attributeSchemaJson) ? "[]" : attributeSchemaJson;
            SetUpdated();
        }
    }
}
