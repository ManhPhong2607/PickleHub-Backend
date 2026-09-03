using PickleHub.Common.Interfaces;
using MassTransit;
using PickleHub.Common.Events.Authen;
using PickleHub.Customers.Domain.Repositories;

namespace PickleHub.Customers.Infrastructure.Consumers
{
    public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserRegisteredConsumer> _logger;

        public UserRegisteredConsumer(
            ICustomerRepository customerRepository,
            IUnitOfWork unitOfWork,
            ILogger<UserRegisteredConsumer> logger)
        {
            _customerRepository = customerRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
        {
            var message = context.Message;

            _logger.LogInformation(
                "Processing UserRegisteredEvent for UserId: {UserId}",
                message.UserId);

            // Idempotent — tránh tạo duplicate nếu nhận event 2 lần
            var existed = await _customerRepository
                .ExistsByUserIdAsync(message.UserId, context.CancellationToken);

            if (existed)
            {
                _logger.LogWarning(
                    "Customer already exists for UserId: {UserId}. Skipping.",
                    message.UserId);
                return;
            }

            // Tạo customer record với thông tin cơ bản từ event
            // FullName tạm dùng phần đầu email, customer tự cập nhật sau
            var fullName = message.Email.Split('@')[0];

            var customer = Domain.Entities.Customer.Create(
                message.UserId,
                message.Email,
                fullName);

            _customerRepository.Add(customer);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation(
                "Created customer record for UserId: {UserId}", message.UserId);
        }
    }
}
