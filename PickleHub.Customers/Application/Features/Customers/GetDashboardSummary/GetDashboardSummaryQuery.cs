using MediatR;
using PickleHub.Customers.Domain.Repositories;

namespace PickleHub.Customers.Application.Features.Customers.GetDashboardSummary
{
    public record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;

    public record DashboardSummaryDto(
        int NewCustomersThisWeek,
        int TotalCustomers,
        int TotalBlocked);

    public class GetDashboardSummaryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
    {
        private readonly ICustomerRepository _customerRepository;

        public GetDashboardSummaryHandler(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken ct)
        {
            var newThisWeek = await _customerRepository.CountNewThisWeekAsync(ct);
            var total = await _customerRepository.CountTotalAsync(ct);
            var blocked = await _customerRepository.CountBlockedAsync(ct);

            return new DashboardSummaryDto(newThisWeek, total, blocked);
        }
    }
}
