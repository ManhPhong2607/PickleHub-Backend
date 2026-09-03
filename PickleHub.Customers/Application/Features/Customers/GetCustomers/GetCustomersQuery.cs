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

        public GetCustomersHandler(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async Task<PagedResult<CustomerSummaryDto>> Handle(GetCustomersQuery request, CancellationToken ct)
        {
            var (items, totalItems) = await _customerRepository.GetPagedAsync(
                request.Keyword,
                request.IsBlocked,
                request.Page,
                request.PageSize,
                ct);

            return new PagedResult<CustomerSummaryDto>
            {
                Items = items.Select(c => new CustomerSummaryDto
                {
                    Id = c.Id,
                    Email = c.Email,
                    FullName = c.FullName,
                    PhoneNumber = c.PhoneNumber,
                    IsBlocked = c.IsBlocked,
                    CreatedAt = c.CreatedAt
                }).ToList(),
                Page = request.Page,
                PageSize = request.PageSize,
                TotalItems = totalItems
            };
        }
    }
}
