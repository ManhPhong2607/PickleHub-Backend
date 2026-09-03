using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.CartOrder.Application.Common.Interfaces;
using PickleHub.CartOrder.Application.Features.Orders.DTOs;

namespace PickleHub.CartOrder.Application.Features.Orders.GetMyOrders;

// Query lấy danh sách đơn hàng (tóm tắt) của User đang đăng nhập.
public record GetMyOrdersQuery(Guid UserId) : IRequest<List<OrderSummaryDto>>;

public class GetMyOrdersQueryHandler(ICartOrderDbContext db) 
    : IRequestHandler<GetMyOrdersQuery, List<OrderSummaryDto>>
{
    public async Task<List<OrderSummaryDto>> Handle(GetMyOrdersQuery request, CancellationToken ct)
    {
        var orders = await db.Orders
            .Include(o => o.Items)
            .Where(o => o.CustomerId == request.UserId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        return orders.Select(o =>
        {
            var firstItem = o.Items.FirstOrDefault();
            return new OrderSummaryDto
            {
                Id = o.Id,
                Status = o.Status.ToString(),
                PaymentMethod = o.PaymentMethod,
                PaymentStatus = o.PaymentStatus.ToString(),
                TotalAmount = o.TotalAmount,
                ItemCount = o.Items.Sum(i => i.Quantity),
                FirstProductId = firstItem?.ProductId,
                FirstItemName = firstItem?.ProductNameSnapshot ?? string.Empty,
                FirstItemImage = firstItem?.ImageUrlSnapshot,
                Items = o.Items.Select(i => new OrderItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductVariantId = i.ProductVariantId != Guid.Empty ? i.ProductVariantId : i.ProductId,
                    ProductNameSnapshot = i.ProductNameSnapshot,
                    VariantAttributesSnapshot = i.VariantAttributesSnapshot,
                    ImageUrlSnapshot = i.ImageUrlSnapshot,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    Subtotal = i.Subtotal
                }).ToList(),
                CreatedAt = o.CreatedAt
            };
        }).ToList();
    }
}
