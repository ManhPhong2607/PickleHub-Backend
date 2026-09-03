using PickleHub.System.Domain.Entities;

namespace PickleHub.System.Domain.Repositories
{
    public interface ISiteAnnouncementRepository
    {
        Task<SiteAnnouncement?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<List<SiteAnnouncement>> GetAllAsync(CancellationToken ct = default);
        Task<List<SiteAnnouncement>> GetActiveAsync(CancellationToken ct = default);
        void Add(SiteAnnouncement announcement);
        void Update(SiteAnnouncement announcement);
        void Remove(SiteAnnouncement announcement);
    }
}
