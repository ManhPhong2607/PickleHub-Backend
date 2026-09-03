namespace PickleHub.Catalog.Application.Features.Categories.DTOs
{
    public class CategoryTreeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
        public string? Url { get; set; }
        public string? PublicId { get; set; }
        public string AttributeSchemaJson { get; set; } = "[]";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<CategoryTreeDto> Children { get; set; } = new();
    }
}
