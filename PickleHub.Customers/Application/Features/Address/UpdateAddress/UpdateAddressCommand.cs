using MediatR;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;
using PickleHub.Customers.Application.Features.DTOs;
using PickleHub.Customers.Domain.Repositories;

namespace PickleHub.Customers.Application.Features.Address.UpdateAddress
{
    public record UpdateAddressCommand(
        Guid AddressId,
        string FullName,
        string PhoneNumber,
        string Province,
        string District,
        string Ward,
        string StreetAddress
        ) : IRequest<AddressDto>;

    public class UpdateAddressHandler : IRequestHandler<UpdateAddressCommand, AddressDto>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ICustomerAddressRepository _addressRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public UpdateAddressHandler(
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

        public async Task<AddressDto> Handle(UpdateAddressCommand request, CancellationToken ct)
        {
            var customer = await _customerRepository.GetByUserIdAsync(_currentUser.UserId, ct)
                ?? throw new NotFoundException("Không tìm thấy thông tin khách hàng.");

            // Chỉ cho sửa địa chỉ của chính mình
            var address = await _addressRepository
                .GetByIdAndCustomerIdAsync(request.AddressId, customer.Id, ct)
                ?? throw new NotFoundException("Không tìm thấy địa chỉ.");

            address.Update(
                request.FullName,
                request.PhoneNumber,
                request.Province,
                request.District,
                request.Ward,
                request.StreetAddress);

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
