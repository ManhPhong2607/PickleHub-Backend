using MassTransit;
using MediatR;
using PickleHub.Common.Events.Order;
using PickleHub.Inventory.Application.Features.Inventory.ReleaseStock;

namespace PickleHub.Inventory.Infrastructure.Consumers;

public class OrderCheckoutFailedConsumer : IConsumer<OrderCheckoutFailedEvent>
{
    private readonly ISender _mediator;
    private readonly ILogger<OrderCheckoutFailedConsumer> _logger;

    public OrderCheckoutFailedConsumer(ISender mediator, ILogger<OrderCheckoutFailedConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCheckoutFailedEvent> context)
    {
        var message = context.Message;
        if (message.Items == null || message.Items.Count == 0)
        {
            _logger.LogInformation("[OrderCheckoutFailedConsumer] Đơn hàng {OrderId} không có item nào cần nhả kho.", message.OrderId);
            return;
        }
        
        _logger.LogWarning("[OrderCheckoutFailedConsumer] Nhận tín hiệu bù trừ cho OrderId {OrderId}. Lý do: {Reason}. Đang nhả kho {Count} sản phẩm...",
            message.OrderId, message.Reason, message.Items.Count);
        
        var releaseItems = message.Items.Select(i => new ReleaseStockItem(i.ProductVariantId, i.Quantity)).ToList();
        await _mediator.Send(new ReleaseStockCommand(message.OrderId, releaseItems), context.CancellationToken);
        
        _logger.LogInformation("[OrderCheckoutFailedConsumer] Hoàn tất nhả tồn kho bù trừ thành công cho OrderId: {OrderId}", 
            message.OrderId);
    }
}