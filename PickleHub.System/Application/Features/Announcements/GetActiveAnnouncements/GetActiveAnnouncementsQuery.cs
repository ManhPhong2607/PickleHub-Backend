using MediatR;
using PickleHub.System.Application.Features.DTOs;
using PickleHub.System.Domain.Repositories;

namespace PickleHub.System.Application.Features.Announcements.GetActiveAnnouncements
{
    public record GetActiveAnnouncementsQuery : IRequest<List<SiteAnnouncementDto>>;

    public class GetActiveAnnouncementsQueryHandler : IRequestHandler<GetActiveAnnouncementsQuery, List<SiteAnnouncementDto>>
    {
        private readonly ISiteAnnouncementRepository _announcementRepository;

        public GetActiveAnnouncementsQueryHandler(ISiteAnnouncementRepository announcementRepository)
        {
            _announcementRepository = announcementRepository;
        }

        public async Task<List<SiteAnnouncementDto>> Handle(GetActiveAnnouncementsQuery request, CancellationToken ct)
        {
            var announcements = await _announcementRepository.GetActiveAsync(ct);
            return announcements.Select(a => new SiteAnnouncementDto
            {
                Id = a.Id,
                Title = a.Title,
                Content = a.Content,
                IsActive = a.IsActive,
                IsVisible = a.IsVisible,
                StartsAt = a.StartsAt,
                EndsAt = a.EndsAt,
                ImageUrl = a.ImageUrl,
                ImagePublicId = a.ImagePublicId,
                CtaLink = a.CtaLink,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt
            }).ToList();
        }
    }
}
