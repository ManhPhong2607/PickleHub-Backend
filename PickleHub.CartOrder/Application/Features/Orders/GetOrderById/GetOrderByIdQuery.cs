using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.CartOrder.Application.Common.Interfaces;
using PickleHub.CartOrder.Application.Features.Orders.DTOs;

namespace PickleHub.CartOrder.Application.Features.Orders.GetOrderById;

// Query lấy chi tiết một đơn hàng theo ID (bảo mật theo UserId của chủ đơn).
public record GetOrderByIdQuery(Guid OrderId, Guid UserId) : IRequest<OrderDto>;

public class GetOrderByIdQueryHandler(ICartOrderDbContext db) 
    : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken ct)
    {
        // Truy vấn đơn hàng kèm các item trong đơn, lọc chính xác theo OrderId và UserId chủ đơn
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.CustomerId == request.UserId, ct);

        if (order is null)
        {
            throw new KeyNotFoundException("Không tìm thấy đơn hàng yêu cầu hoặc bạn không có quyền xem đơn hàng này.");
        }

        return new OrderDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            Status = order.Status.ToString(),
            PaymentMethod = order.PaymentMethod,
            PaymentStatus = order.PaymentStatus.ToString(),
            
            ShippingFullName = order.ShippingFullName,
            ShippingPhone = order.ShippingPhone,
            ShippingProvince = order.ShippingProvince,
            ShippingDistrict = order.ShippingDistrict,
            ShippingWard = order.ShippingWard,
            ShippingStreetAddress = order.ShippingStreetAddress,
            
            ShippingProvider = order.ShippingProvider,
            TrackingNumber = order.TrackingNumber,
            TrackingUrl = order.TrackingUrl,
            
            Subtotal = order.Subtotal,
            LoyaltyDiscountPercent = order.LoyaltyDiscountPercent,
            LoyaltyDiscountAmount = order.LoyaltyDiscountAmount,
            ShippingFee = order.ShippingFee,
            TotalAmount = order.TotalAmount,
            
            CancelledBy = order.CancelledBy,
            CancelReason = order.CancelReason,
            
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,

            Items = order.Items.Select(item => new OrderItemDto
            {
                Id = item.Id,
                ProductVariantId = item.ProductVariantId != Guid.Empty ? item.ProductVariantId : item.ProductId,
                ProductId = item.ProductId,
                ProductNameSnapshot = item.ProductNameSnapshot,
                VariantAttributesSnapshot = item.VariantAttributesSnapshot,
                ImageUrlSnapshot = item.ImageUrlSnapshot,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                Subtotal = item.Subtotal
            }).ToList()
        };
    }
}
