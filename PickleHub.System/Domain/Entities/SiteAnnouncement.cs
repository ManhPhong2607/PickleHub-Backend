using PickleHub.Common.Domain;

namespace PickleHub.System.Domain.Entities
{
    public class SiteAnnouncement : BaseEntity
    {
        public string Title { get; private set; } = string.Empty;
        public string Content { get; private set; } = string.Empty;
        public bool IsActive { get; private set; } = true;

        public string? ImageUrl { get; private set; }
        public string? ImagePublicId { get; private set; }
        public string? CtaLink { get; private set; }
        public DateTime? StartsAt { get; private set; }
        public DateTime? EndsAt { get; private set; }
        public bool IsVisible =>
            IsActive && (StartsAt == null || StartsAt <= DateTime.UtcNow)
                     && (EndsAt == null || EndsAt >= DateTime.UtcNow);

        private SiteAnnouncement() { }
        public static SiteAnnouncement Create(
            string title,
            string content,
            bool isActive = true,
            DateTime? startsAt = null,
            DateTime? endsAt = null,
            string? imageUrl = null,
            string? imagePublicId = null,
            string? ctaLink = null
            )
        {
            return new SiteAnnouncement
            {
                Title = title.Trim(),
                Content = content.Trim(),
                IsActive = isActive,
                StartsAt = startsAt.HasValue ? DateTime.SpecifyKind(startsAt.Value, DateTimeKind.Utc) : null,
                EndsAt = endsAt.HasValue ? DateTime.SpecifyKind(endsAt.Value, DateTimeKind.Utc) : null,
                ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim(),
                ImagePublicId = string.IsNullOrWhiteSpace(imagePublicId) ? null : imagePublicId.Trim(),
                CtaLink = string.IsNullOrWhiteSpace(ctaLink) ? null : ctaLink.Trim()
            };
        }

        public void Update(
            string title,
            string content,
            bool isActive,
            DateTime? startsAt,
            DateTime? endsAt,
            string? imageUrl,
            string? imagePublicId,
            string? ctaLink
            )
        {
            Title = title.Trim();
            Content = content.Trim();
            IsActive = isActive;
            StartsAt = startsAt.HasValue ? DateTime.SpecifyKind(startsAt.Value, DateTimeKind.Utc) : null;
            EndsAt = endsAt.HasValue ? DateTime.SpecifyKind(endsAt.Value, DateTimeKind.Utc) : null;
            ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
            ImagePublicId = string.IsNullOrWhiteSpace(imagePublicId) ? null : imagePublicId.Trim();
            CtaLink = string.IsNullOrWhiteSpace(ctaLink) ? null : ctaLink.Trim();
            SetUpdated();
        }

        public void Activate()
        {
            IsActive = true;
            SetUpdated();
        }

        public void Deactivate()
        {
            IsActive = false;
            SetUpdated();
        }
    }
}
