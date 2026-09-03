using MediatR;
using PickleHub.Common.Exceptions;
using PickleHub.Customers.Application.Features.DTOs;
using PickleHub.Customers.Domain.Repositories;

namespace PickleHub.Customers.Application.Features.Address.GetAddressById
{
    public record GetAddressByIdQuery(Guid AddressId) : IRequest<AddressDto>;

    public class GetAddressByIdHandler : IRequestHandler<GetAddressByIdQuery, AddressDto>
    {
        private readonly ICustomerAddressRepository _addressRepository;

        public GetAddressByIdHandler(ICustomerAddressRepository addressRepository)
        {
            _addressRepository = addressRepository;
        }

        public async Task<AddressDto> Handle(GetAddressByIdQuery request, CancellationToken ct)
        {
            var address = await _addressRepository.GetByIdAsync(request.AddressId, ct)
                ?? throw new NotFoundException("Không tìm thấy địa chỉ.");

            return new AddressDto
            {
                Id = address.Id,
                FullName = address.FullName,
                PhoneNumber = address.PhoneNumber,
                Province = address.Province,
                District = address.District,
                Ward = address.Ward,
                StreetAddress = address.StreetAddress,
                IsDefault = address.IsDefault
            };
        }
    }
}
