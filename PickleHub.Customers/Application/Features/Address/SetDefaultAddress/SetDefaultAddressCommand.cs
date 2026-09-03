using MediatR;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;
using PickleHub.Customers.Application.Features.DTOs;
using PickleHub.Customers.Domain.Repositories;

namespace PickleHub.Customers.Application.Features.Address.SetDefaultAddress
{
    public record SetDefaultAddressCommand(Guid AddressId) : IRequest<AddressDto>;

    public class SetDefaultAddressHandler : IRequestHandler<SetDefaultAddressCommand, AddressDto>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ICustomerAddressRepository _addressRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public SetDefaultAddressHandler(
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

        public async Task<AddressDto> Handle(SetDefaultAddressCommand request, CancellationToken ct)
        {
            var customer = await _customerRepository.GetByUserIdAsync(_currentUser.UserId, ct)
                ?? throw new NotFoundException("Không tìm thấy thông tin khách hàng.");

            var address = await _addressRepository
                .GetByIdAndCustomerIdAsync(request.AddressId, customer.Id, ct)
                ?? throw new NotFoundException("Không tìm thấy địa chỉ.");

            // Unset tất cả default cũ
            var currentDefaults = await _addressRepository
                .GetDefaultsByCustomerIdAsync(customer.Id, ct);

            foreach (var d in currentDefaults)
                d.UnsetDefault();

            address.SetAsDefault();
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
