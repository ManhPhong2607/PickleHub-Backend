using PickleHub.Common.Interfaces;
using MediatR;
using PickleHub.Common.Exceptions;
using PickleHub.System.Domain.Repositories;
using System.Net.WebSockets;

namespace PickleHub.System.Application.Features.Announcements.DeleteAnnouncement
{
    public record DeleteAnnouncementCommand(Guid AnnouncementId) : IRequest;
    public class DeleteAnnouncementHandler : IRequestHandler<DeleteAnnouncementCommand>
    {
        private readonly ISiteAnnouncementRepository _announcementRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStorageService _storage;
        private readonly ILogger<DeleteAnnouncementHandler> _logger;

        public DeleteAnnouncementHandler(
            ISiteAnnouncementRepository announcementRepository,
            IUnitOfWork unitOfWork,
            IStorageService storage,
            ILogger<DeleteAnnouncementHandler> logger)
        {
            _announcementRepository = announcementRepository;
            _unitOfWork = unitOfWork;
            _storage = storage;
            _logger = logger;
        }

        public async Task Handle(DeleteAnnouncementCommand request, CancellationToken ct)
        {
            var announcement = await _announcementRepository.GetByIdAsync(request.AnnouncementId, ct)
                ?? throw new NotFoundException("Không tìm thấy thông báo.");
            var publicId = announcement.ImagePublicId;

            _announcementRepository.Remove(announcement);
            await _unitOfWork.SaveChangesAsync(ct);
            if(!string.IsNullOrEmpty(publicId))
            {
                try
                {
                    await _storage.DeleteAsync(publicId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "không xóa được ảnh [{PublicId}] khỏi Cloudinary khi xóa announcement [{Announcement}].", publicId, request.AnnouncementId);
                }
            }
        }
    }
}
