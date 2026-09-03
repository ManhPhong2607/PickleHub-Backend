using MediatR;
using PickleHub.Common.DTOs;
using PickleHub.Customers.Application.Features.DTOs;
using PickleHub.Customers.Domain.Repositories;

namespace PickleHub.Customers.Application.Features.Customers.GetCustomers
{
    public record GetCustomersQuery(
        string? Keyword,
        bool? IsBlocked,
        int Page = 1,
        int PageSize = 20
    ) : IRequest<PagedResult<CustomerSummaryDto>>;

    public class GetCustomersHandler : IRequestHandler<GetCustomersQuery, PagedResult<CustomerSummaryDto>>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ILoyaltyTierRepository _loyaltyTierRepository;

        public GetCustomersHandler(
            ICustomerRepository customerRepository,
            ILoyaltyTierRepository loyaltyTierRepository)
        {
            _customerRepository = customerRepository;
            _loyaltyTierRepository = loyaltyTierRepository;
        }

        public async Task<PagedResult<CustomerSummaryDto>> Handle(GetCustomersQuery request, CancellationToken ct)
        {
            var (items, totalItems) = await _customerRepository.GetPagedAsync(
                request.Keyword,
                request.IsBlocked,
                request.Page,
                request.PageSize,
                ct);

            var tiers = await _loyaltyTierRepository.GetAllOrderedAsync(ct);

            return new PagedResult<CustomerSummaryDto>
            {
                Items = items.Select(c =>
                {
                    var currentTier = Domain.Services.LoyaltyTierCalculator.GetCurrentTier(c.TotalSpent, tiers);
                    return new CustomerSummaryDto
                    {
                        Id = c.Id,
                        UserId = c.UserId,
                        Email = c.Email,
                        FullName = c.FullName,
                        PhoneNumber = c.PhoneNumber,
                        IsBlocked = c.IsBlocked,
                        TotalSpent = c.TotalSpent,
                        LoyaltyTierName = currentTier?.Name ?? "Thành viên mới",
                        LoyaltyDiscountPercent = currentTier?.DiscountPercent ?? 0,
                        CreatedAt = c.CreatedAt
                    };
                }).ToList(),
                Page = request.Page,
                PageSize = request.PageSize,
                TotalItems = totalItems
            };
        }
    }
}
