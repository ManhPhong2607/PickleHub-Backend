using Microsoft.EntityFrameworkCore;
using PickleHub.System.Domain.Entities;
using PickleHub.System.Domain.Repositories;

namespace PickleHub.System.Infrastructure.Persistence.Repositories
{
    public class SiteAnnouncementRepository : ISiteAnnouncementRepository
    {
        private readonly SystemDbContext _db;

        public SiteAnnouncementRepository(SystemDbContext db)
        {
            _db = db;
        }

        public async Task<SiteAnnouncement?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.SiteAnnouncements.FirstOrDefaultAsync(a => a.Id == id, ct);
        }

        public async Task<List<SiteAnnouncement>> GetAllAsync(CancellationToken ct = default)
        {
            return await _db.SiteAnnouncements
                .AsNoTracking()
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<List<SiteAnnouncement>> GetActiveAsync(CancellationToken ct = default)
        {
           return await _db.SiteAnnouncements
                .AsNoTracking()
                .Where(a =>
                    a.IsActive &&
                    (a.StartsAt == null || a.StartsAt <= DateTime.UtcNow) &&
                    (a.EndsAt == null || a.EndsAt >= DateTime.UtcNow))
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(ct);
        }
        public void Add(SiteAnnouncement announcement)
        {
            _db.SiteAnnouncements.Add(announcement);
        }

        public void Update(SiteAnnouncement announcement)
        {
            _db.SiteAnnouncements.Update(announcement);
        }

        public void Remove(SiteAnnouncement announcement)
        {
            _db.SiteAnnouncements.Remove(announcement);
        }
    }
}
