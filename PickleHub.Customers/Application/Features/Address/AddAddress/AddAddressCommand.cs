using MediatR;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;
using PickleHub.Customers.Application.Features.DTOs;
using PickleHub.Customers.Domain.Entities;
using PickleHub.Customers.Domain.Repositories;

namespace PickleHub.Customers.Application.Features.Address.AddAdress
{
    public record AddAddressCommand(
        string FullName,
        string PhoneNumber,
        string Province,
        string District,
        string Ward,
        string StreetAddress,
        bool IsDefault = false) : IRequest<AddressDto>;

    public class AddAddressHandler : IRequestHandler<AddAddressCommand, AddressDto>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ICustomerAddressRepository _addressRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public AddAddressHandler(
            ICustomerRepository customerRepository,
            ICustomerAddressRepository addressRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _customerRepository = customerRepository;
            _addressRepository = addressRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<AddressDto> Handle(AddAddressCommand request, CancellationToken ct)
        {
            var customer = await _customerRepository.GetByUserIdAsync(_currentUser.UserId, ct)
                ?? throw new NotFoundException("Không tìm thấy thông tin khách hàng.");

            // Nếu set làm default → unset tất cả địa chỉ cũ
            if (request.IsDefault)
            {
                var existingDefaults = await _addressRepository
                    .GetDefaultsByCustomerIdAsync(customer.Id, ct);

                foreach (var d in existingDefaults)
                    d.UnsetDefault();
            }

            var address = CustomerAddress.Create(
                customer.Id,
                request.FullName,
                request.PhoneNumber,
                request.Province,
                request.District,
                request.Ward,
                request.StreetAddress,
                request.IsDefault);

            _addressRepository.Add(address);
            await _unitOfWork.SaveChangesAsync(ct);

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
