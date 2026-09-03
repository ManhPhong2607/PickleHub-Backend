using PickleHub.Common.Domain;
using PickleHub.Common.Exceptions;
using PickleHub.Common.ValueObjects;

namespace PickleHub.Blog.Domain.Entities
{
    public class ContentCategory : BaseEntity
    {
        public string Name { get; private set; } = null!;
        public Slug Slug { get; private set; } = null!;
        public string? Description { get; private set; }
        public int DisplayOrder { get; private set; }

        private ContentCategory() { } 

        public static ContentCategory Create(string name, Slug slug, string? description, int displayOrder = 0)
        {
            if(string.IsNullOrWhiteSpace(name))
                throw new DomainException("Tên category không được để trống.");

            return new ContentCategory
            {
                Name = name.Trim(),
                Slug = slug,
                Description = description,
                DisplayOrder = displayOrder
            };
        }

        public void Update(string name, Slug slug, string? description, int displayOrder)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Tên category không được để trống.");

            Name = name.Trim();
            Slug = slug;
            Description = description;
            DisplayOrder = displayOrder;
            SetUpdated();
        }
    }
}
