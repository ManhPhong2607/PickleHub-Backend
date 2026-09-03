using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.CartOrder.Application.Common.Interfaces;
using PickleHub.Common.Enums;

namespace PickleHub.CartOrder.Application.Features.Orders.VerifyOrder;

public record VerifyOrderQuery(
    Guid OrderId,
    Guid UserId,
    Guid ProductId
) : IRequest<VerifyOrderResponseDto>;

public record VerifyOrderResponseDto(bool IsCompleted);

public class VerifyOrderQueryHandler(ICartOrderDbContext db) : IRequestHandler<VerifyOrderQuery, VerifyOrderResponseDto>
{
    public async Task<VerifyOrderResponseDto> Handle(VerifyOrderQuery request, CancellationToken ct)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.CustomerId == request.UserId, ct);

        if (order == null)
        {
            return new VerifyOrderResponseDto(false);
        }

        // Kiểm tra xem đơn hàng đã được thanh toán hoặc hoàn thành chưa
        bool isPaidOrCompleted = order.PaymentStatus == PaymentStatus.Paid 
            || order.Status == OrderStatus.Completed 
            || order.Status == OrderStatus.Shipping 
            || order.Status == OrderStatus.Confirmed;

        // Kiểm tra xem sản phẩm có nằm trong đơn hàng không (nếu order có items, kiểm tra ProductId hoặc ProductVariantId; nếu không có items chi tiết thì coi như hợp lệ)
        bool containsProduct = order.Items == null || order.Items.Count == 0 || order.Items.Any(i => i.ProductId == request.ProductId || i.ProductVariantId == request.ProductId);

        return new VerifyOrderResponseDto(isPaidOrCompleted && containsProduct);
    }
}
