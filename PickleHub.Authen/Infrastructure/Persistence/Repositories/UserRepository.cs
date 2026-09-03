using Microsoft.EntityFrameworkCore;
using PickleHub.Authen.Domain.Entities;
using PickleHub.Authen.Domain.Enums;
using PickleHub.Authen.Domain.Repositories;

namespace PickleHub.Authen.Infrastructure.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AuthenDbContext _db;

        public UserRepository(AuthenDbContext db)
        {
            _db = db;
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
             return await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        }
           
        public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        {
             return await _db.Users.FirstOrDefaultAsync(
                u => u.Email == email.Trim().ToLowerInvariant(), ct);
        }
           
        public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        {
             return await _db.Users.AnyAsync(
                u => u.Email == email.Trim().ToLowerInvariant(), ct);
        }
        public async Task<bool> AnyAdminAsync(CancellationToken ct = default)
        {
            return await _db.Users.AnyAsync(u => u.Role == UserRole.Admin, ct);
        }
        public void Add(User user) 
        {
            _db.Users.Add(user);
        }

        public void Update(User user) 
        {
            _db.Users.Update(user);
        }
    
    }
}
