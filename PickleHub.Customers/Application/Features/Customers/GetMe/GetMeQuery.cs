using MediatR;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;
using PickleHub.Customers.Application.Features.DTOs;
using PickleHub.Customers.Domain.Repositories;

namespace PickleHub.Customers.Application.Features.Customers.GetMe
{
    public record GetMeQuery : IRequest<CustomerDto>;

    public class GetMeHandler : IRequestHandler<GetMeQuery, CustomerDto>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ICurrentUserService _currentUser;

        public GetMeHandler(
            ICustomerRepository customerRepository,
            ICurrentUserService currentUser)
        {
            _customerRepository = customerRepository;
            _currentUser = currentUser;
        }

        public async Task<CustomerDto> Handle(GetMeQuery request, CancellationToken ct)
        {
            var customer = await _customerRepository.GetByUserIdAsync(_currentUser.UserId, ct)
                ?? throw new NotFoundException("Không tìm thấy thông tin khách hàng.");

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
