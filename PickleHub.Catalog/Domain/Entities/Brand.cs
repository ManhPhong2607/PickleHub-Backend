using PickleHub.Common.Domain;
using PickleHub.Common.Exceptions;
using PickleHub.Common.ValueObjects;

namespace PickleHub.Catalog.Domain.Entities
{
    public class Brand : BaseEntity
    {
        public string Name { get; private set; } = string.Empty;
        public Slug Slug { get; private set; } = null!;
        private Brand() { }

        public static Brand Create(string name, Slug slug)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Tên thương hiệu không được để trống.");
            return new Brand 
            { 
                Name = name.Trim(),
                Slug = slug
            };
        }

        public void Update(string name, Slug slug)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Tên thương hiệu không được để trống.");
            Name = name;
            Slug = slug;
            SetUpdated();
        }
    }
}
