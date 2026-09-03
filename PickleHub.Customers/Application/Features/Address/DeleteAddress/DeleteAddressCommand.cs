using MediatR;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;
using PickleHub.Customers.Domain.Repositories;

namespace PickleHub.Customers.Application.Features.Address.DeleteAddress
{
    public record DeleteAddressCommand(Guid AddressId) : IRequest;

    public class DeleteAddressHandler : IRequestHandler<DeleteAddressCommand>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ICustomerAddressRepository _addressRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public DeleteAddressHandler(
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

        public async Task Handle(DeleteAddressCommand request, CancellationToken ct)
        {
            var customer = await _customerRepository.GetByUserIdAsync(_currentUser.UserId, ct)
                ?? throw new NotFoundException("Không tìm thấy thông tin khách hàng.");

            var address = await _addressRepository
                .GetByIdAndCustomerIdAsync(request.AddressId, customer.Id, ct)
                ?? throw new NotFoundException("Không tìm thấy địa chỉ.");

            if (address.IsDefault)
                throw new ConflictException("Không thể xóa địa chỉ mặc định. Vui lòng đặt địa chỉ khác làm mặc định trước.");

            _addressRepository.Remove(address);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
