using Microsoft.EntityFrameworkCore;
using PickleHub.Payment.Domain.Entities;

namespace PickleHub.Payment.Application.Common.Interfaces;

public interface IPaymentDbContext
{
    DbSet<Payments> Payments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
