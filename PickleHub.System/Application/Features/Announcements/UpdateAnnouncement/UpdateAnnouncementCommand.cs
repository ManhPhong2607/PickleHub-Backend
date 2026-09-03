using PickleHub.Common.Interfaces;
using MediatR;
using PickleHub.Common.Exceptions;
using PickleHub.System.Application.Features.DTOs;
using PickleHub.System.Domain.Repositories;

namespace PickleHub.System.Application.Features.Announcements.UpdateAnnouncement
{
    public record UpdateAnnouncementCommand(
       Guid AnnouncementId,
       string Title,
       string Content,
       bool IsActive,
       DateTime? StartsAt,
       DateTime? EndsAt,
       string? ImageUrl,
       string? ImagePublicId,
       string? CtaLink
    ) : IRequest<SiteAnnouncementDto>;

    public class UpdateAnnouncementHandler : IRequestHandler<UpdateAnnouncementCommand, SiteAnnouncementDto>
    {
        private readonly ISiteAnnouncementRepository _announcementRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStorageService _storage;
        private readonly ILogger<UpdateAnnouncementHandler> _logger;
        public UpdateAnnouncementHandler(
           ISiteAnnouncementRepository announcementRepository,
           IUnitOfWork unitOfWork,
           IStorageService storage,
           ILogger<UpdateAnnouncementHandler> logger)
        {
            _announcementRepository = announcementRepository;
            _unitOfWork = unitOfWork;
            _storage = storage;
            _logger = logger;
        }
        public async Task<SiteAnnouncementDto> Handle(UpdateAnnouncementCommand request, CancellationToken ct)
        {
            var announcement = await _announcementRepository.GetByIdAsync(request.AnnouncementId, ct)
                ?? throw new NotFoundException("Không tìm thấy thông báo.");

            var oldPublicId = announcement.ImagePublicId;
            var isImageChanged = !string.IsNullOrEmpty(oldPublicId) && oldPublicId != request.ImagePublicId;
            announcement.Update(
                request.Title,
                request.Content,
                request.IsActive,
                request.StartsAt,
                request.EndsAt,
                request.ImageUrl,
                request.ImagePublicId,
                request.CtaLink
            );

            await _unitOfWork.SaveChangesAsync(ct);
            if (isImageChanged)
            {
                try
                {
                    await _storage.DeleteAsync(oldPublicId!);
                }
                catch(Exception ex)
                {
                    _logger.LogWarning(ex, "Không xóa được ảnh cũ [{PublicId}] khỏi Cloudinary khi update announcement [{AnnouncementId}].", oldPublicId, announcement.Id);
                }
            }
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
