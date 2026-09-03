using PickleHub.Common.Interfaces;
using MediatR;
using PickleHub.System.Application.Features.DTOs;
using PickleHub.System.Domain.Entities;
using PickleHub.System.Domain.Repositories;

namespace PickleHub.System.Application.Features.Announcements.CreateAnnouncement
{
    public record CreateAnnouncementCommand(
        string Title,
        string Content,
        bool IsActive = true,
        DateTime? StartsAt = null,
        DateTime? EndsAt = null,
        string? ImageUrl = null,
        string? ImagePublicId = null,
        string? CtaLink = null
    ) : IRequest<SiteAnnouncementDto>;

    public class CreateAnnouncementHandler : IRequestHandler<CreateAnnouncementCommand, SiteAnnouncementDto>
    {
        private readonly ISiteAnnouncementRepository _announcementRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateAnnouncementHandler(ISiteAnnouncementRepository announcementRepository, IUnitOfWork unitOfWork)
        {
            _announcementRepository = announcementRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<SiteAnnouncementDto> Handle(CreateAnnouncementCommand request, CancellationToken ct)
        {
            var announcement =  SiteAnnouncement.Create(
                request.Title,
                request.Content,
                request.IsActive,
                request.StartsAt,
                request.EndsAt,
                request.ImageUrl,
                request.ImagePublicId,
                request.CtaLink
            );
            _announcementRepository.Add(announcement);
            await _unitOfWork.SaveChangesAsync(ct);

            return new SiteAnnouncementDto
            {
                Id = announcement.Id,
                Title = announcement.Title,
                Content = announcement.Content,
                IsActive = announcement.IsActive,
                IsVisible = announcement.IsVisible,
                StartsAt = announcement.StartsAt,
                EndsAt = announcement.EndsAt,
                ImageUrl = announcement.ImageUrl,
                ImagePublicId = announcement.ImagePublicId,
                CtaLink = announcement.CtaLink,
                CreatedAt = announcement.CreatedAt,
                UpdatedAt = announcement.UpdatedAt
            };        
        }
    }

}
