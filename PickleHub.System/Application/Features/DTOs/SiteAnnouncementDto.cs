namespace PickleHub.System.Application.Features.DTOs
{
    public class SiteAnnouncementDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsVisible { get; set; }
        public DateTime? StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImagePublicId { get; set; }
        public string? CtaLink { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
