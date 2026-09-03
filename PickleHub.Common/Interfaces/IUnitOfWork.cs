using PickleHub.Common.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace PickleHub.Common.Interfaces
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken ct = default);
        void ClearTracking();
    }
}
