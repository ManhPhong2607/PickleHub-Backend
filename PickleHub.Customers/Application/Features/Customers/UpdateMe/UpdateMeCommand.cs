using MediatR;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;
using PickleHub.Customers.Application.Features.DTOs;
using PickleHub.Customers.Domain.Repositories;

namespace PickleHub.Customers.Application.Features.Customers.UpdateMe
{
    public record UpdateMeCommand(string FullName, string? PhoneNumber) : IRequest<CustomerDto>;

    public class UpdateMeHandler : IRequestHandler<UpdateMeCommand, CustomerDto>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public UpdateMeHandler(
            ICustomerRepository customerRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _customerRepository = customerRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<CustomerDto> Handle(UpdateMeCommand request, CancellationToken ct)
        {
            var customer = await _customerRepository.GetByUserIdAsync(_currentUser.UserId, ct)
                ?? throw new NotFoundException("Không tìm thấy thông tin khách hàng.");

            customer.Update(request.FullName, request.PhoneNumber);
            await _unitOfWork.SaveChangesAsync(ct);

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
