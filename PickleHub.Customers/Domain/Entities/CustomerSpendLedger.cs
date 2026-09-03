using PickleHub.Common.Domain;

namespace PickleHub.Customers.Domain.Entities
{
    //sổ ghi nhận tiền khách đã chi.
    public class CustomerSpendLedger : BaseEntity
    {
        // Mỗi Order chỉ được tính vào TotalSpent đúng 1 lần - OrderId có unique index ở DbContext.
        // Nhờ vậy nếu OrderStatusUpdatedEvent bị gửi lại (MassTransit retry/redeliver) thì không bị cộng trùng tiền vào TotalSpent của khách.
        public Guid CustomerId { get; private set; }
        public Guid OrderId { get; private set; }
        public decimal Amount { get; private set; }

        private CustomerSpendLedger() { } 

        public static CustomerSpendLedger Create(Guid customerId, Guid orderId, decimal amount)
        {
            return new CustomerSpendLedger
            {
                CustomerId = customerId,
                OrderId = orderId,
                Amount = amount
            };
        }
    }
}
