namespace PickleHub.Catalog.Application.Features.Categories.DTOs
{
    public class CategoryTreeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
        public string AttributeSchemaJson { get; set; } = "[]";
        public List<CategoryTreeDto> Children { get; set; } = new();
    }
}
