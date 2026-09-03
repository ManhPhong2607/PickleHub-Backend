using PickleHub.Common.Interfaces;
using MediatR;
using PickleHub.Common.Events.Customers;
using PickleHub.Common.Exceptions;
using PickleHub.Customers.Domain.Repositories;
using MassTransit;
namespace PickleHub.Customers.Application.Features.Customers.BlockCustomer
{
    public record BlockCustomerCommand(Guid CustomerId, bool IsBlocked) : IRequest;

    public class BlockCustomerHandler : IRequestHandler<BlockCustomerCommand>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublishEndpoint _publishEndpoint;

        public BlockCustomerHandler(
            ICustomerRepository customerRepository,
            IUnitOfWork unitOfWork,
            IPublishEndpoint publishEndpoint)
        {
            _customerRepository = customerRepository;
            _unitOfWork = unitOfWork;
            _publishEndpoint = publishEndpoint;
        }

        public async Task Handle(BlockCustomerCommand request, CancellationToken ct)
        {
            var customer = await _customerRepository.GetByIdAsync(request.CustomerId, ct)
                ?? throw new NotFoundException("Không tìm thấy khách hàng.");

            if (request.IsBlocked)
                customer.Block();
            else
                customer.Unblock();

            await _unitOfWork.SaveChangesAsync(ct);

            // Publish event để Authen Service đồng bộ trạng thái khoá/mở khoá và thu hồi refresh token nếu bị khoá
            await _publishEndpoint.Publish(new CustomerBlockedEvent
            {
                CustomerId = customer.Id,
                UserId = customer.UserId,
                CustomerEmail = customer.Email,
                IsBlocked = request.IsBlocked,
                OccurredAt = DateTime.UtcNow
            }, ct);
        }
    }
}
