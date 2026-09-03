using System.Threading;
using System.Threading.Tasks;

namespace PickleHub.CartOrder.Domain.Interfaces;

public interface ISystemClient
{
    Task<decimal> GetDefaultShippingFeeAsync(CancellationToken ct = default);
}
