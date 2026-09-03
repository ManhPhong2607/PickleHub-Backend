using MediatR;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;
using PickleHub.Customers.Application.Features.DTOs;
using PickleHub.Customers.Domain.Repositories;

namespace PickleHub.Customers.Application.Features.Address.GetMyAddress
{
    public record GetMyAddressesQuery : IRequest<List<AddressDto>>;

    public class GetMyAddressesHandler : IRequestHandler<GetMyAddressesQuery, List<AddressDto>>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ICustomerAddressRepository _addressRepository;
        private readonly ICurrentUserService _currentUser;

        public GetMyAddressesHandler(
            ICustomerRepository customerRepository,
            ICustomerAddressRepository addressRepository,
            ICurrentUserService currentUser)
        {
            _customerRepository = customerRepository;
            _addressRepository = addressRepository;
            _currentUser = currentUser;
        }

        public async Task<List<AddressDto>> Handle(GetMyAddressesQuery request, CancellationToken ct)
        {
            var customer = await _customerRepository.GetByUserIdAsync(_currentUser.UserId, ct)
                ?? throw new NotFoundException("Không tìm thấy thông tin khách hàng.");

            var addresses = await _addressRepository.GetByCustomerIdAsync(customer.Id, ct);

            return addresses.Select(a => new AddressDto
            {
                Id = a.Id,
                FullName = a.FullName,
                PhoneNumber = a.PhoneNumber,
                Province = a.Province,
                District = a.District,
                Ward = a.Ward,
                StreetAddress = a.StreetAddress,
                IsDefault = a.IsDefault
            }).ToList();
        }
    }
}
