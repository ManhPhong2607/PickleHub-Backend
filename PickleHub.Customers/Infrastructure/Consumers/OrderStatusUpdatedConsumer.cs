using MassTransit;
using Microsoft.EntityFrameworkCore;
using PickleHub.Common.Enums;
using PickleHub.Common.Events.Order;
using PickleHub.Common.Interfaces;
using PickleHub.Customers.Domain.Entities;
using PickleHub.Customers.Domain.Repositories;
using PickleHub.Customers.Infrastructure.Persistence;

namespace PickleHub.Customers.Infrastructure.Consumers
{
    public class OrderStatusUpdatedConsumer : IConsumer<OrderStatusUpdatedEvent>
    { 
        private readonly ICustomerRepository _customerRepository;
        private readonly CustomerDbContext _db;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OrderStatusUpdatedConsumer> _logger;

        public OrderStatusUpdatedConsumer(
            ICustomerRepository customerRepository,
            CustomerDbContext db,
            IUnitOfWork unitOfWork,
            ILogger<OrderStatusUpdatedConsumer> logger)
        {
            _customerRepository = customerRepository;
            _db = db;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderStatusUpdatedEvent> context)
        {
            var message = context.Message;
            // Chỉ tính điểm tích lũy khi đơn thật sự hoàn thành - Pending/Confirmed/Shipping
            // chưa chắc chắn khách đã nhận hàng, Cancelled thì càng không được tính.
            if (message.NewStatus != OrderStatus.Completed)
            {
                return;
            }

            // Chống cộng trùng nếu event bị gửi lại (MassTransit retry/redeliver) -
            // OrderId có unique index ở DbContext nên đơn này đã ghi nhận rồi thì bỏ qua êm.
            var alreadyProcessed = await _db.CustomerSpendLedgers.AnyAsync(l => l.OrderId == message.OrderId, context.CancellationToken);
            if (alreadyProcessed) 
            {
                _logger.LogInformation("OrderId {OrderId} đã được tính vào TotalSpent trước đó. Bỏ qua.", message.OrderId);
                return;
            }

            var customer = await _customerRepository.GetByIdAsync(message.CustomerId, context.CancellationToken);
            if (customer == null) 
            {
                _logger.LogWarning("Không tìm thấy Customer [{CustomerId}] để cộng điểm tích lũy cho Order [{OrderId}", message.CustomerId, message.OrderId);
                return;
            }

            var orderAmount = message.Items.Sum(i => i.Quantity * i.UnitPrice);
            if (orderAmount <= 0) 
            {
                return;
            }

            customer.AddSpend(orderAmount);
            _customerRepository.Update(customer);
            _db.CustomerSpendLedgers.Add(CustomerSpendLedger.Create(customer.Id, message.OrderId, orderAmount));
            await _unitOfWork.SaveChangesAsync(context.CancellationToken);
            _logger.LogInformation("Đã cộng {Amount}đ vào TotalSpent của Customer [{CustomerId}] từ Order [{OrderId}].", orderAmount, customer.Id, message.OrderId);
        }
    }
}
