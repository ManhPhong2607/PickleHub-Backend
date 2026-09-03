using MediatR;
using PickleHub.Common.Exceptions;
using PickleHub.Customers.Application.Features.DTOs;
using PickleHub.Customers.Domain.Repositories;

namespace PickleHub.Customers.Application.Features.Customers.GetCustomerDetail
{
    public record GetCustomerDetailQuery(Guid CustomerId) : IRequest<CustomerDto>;

    public class GetCustomerDetailHandler : IRequestHandler<GetCustomerDetailQuery, CustomerDto>
    {
        private readonly ICustomerRepository _customerRepository;

        public GetCustomerDetailHandler(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async Task<CustomerDto> Handle(GetCustomerDetailQuery request, CancellationToken ct)
        {
            var customer = await _customerRepository.GetByIdAsync(request.CustomerId, ct)
                ?? throw new NotFoundException("Không tìm thấy khách hàng.");

            return new CustomerDto
            {
                Id = customer.Id,
                UserId = customer.UserId,
                Email = customer.Email,
                FullName = customer.FullName,
                PhoneNumber = customer.PhoneNumber,
                AvatarUrl = customer.AvatarUrl,
                IsBlocked = customer.IsBlocked,
                CreatedAt = customer.CreatedAt
            };
        }
    }
}
