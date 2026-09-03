using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.CartOrder.Application.Common.Interfaces;
using PickleHub.CartOrder.Application.Features.Orders.DTOs;
using PickleHub.Common.Enums;
using PickleHub.Common.DTOs;

namespace PickleHub.CartOrder.Application.Features.Orders.GetOrdersAdmin;

// Query lấy danh sách toàn bộ đơn hàng trong hệ thống (dành cho Admin).
// Hỗ trợ lọc theo trạng thái, tìm kiếm từ khóa, và phân trang.
public record GetOrdersAdminQuery(
    OrderStatus? Status,
    string? SearchKeyword,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<OrderSummaryDto>>;

public class GetOrdersAdminQueryHandler(ICartOrderDbContext db) 
    : IRequestHandler<GetOrdersAdminQuery, PagedResult<OrderSummaryDto>>
{
    public async Task<PagedResult<OrderSummaryDto>> Handle(GetOrdersAdminQuery request, CancellationToken ct)
    {
        var query = db.Orders.Include(o => o.Items).AsQueryable();

        if (request.Status.HasValue)
        {
            query = query.Where(o => o.Status == request.Status.Value);
        }

        //Tìm kiếm theo Tên khách hàng nhận hoặc Số điện thoại (nếu có)
        if (!string.IsNullOrWhiteSpace(request.SearchKeyword))
        {
            var keyword = request.SearchKeyword.Trim().ToLower();
            query = query.Where(o => o.ShippingFullName.ToLower().Contains(keyword) || 
                                     o.ShippingPhone.Contains(keyword));
        }

        //Đếm tổng số bản ghi thỏa mãn điều kiện lọc
        var totalItems = await query.CountAsync(ct);

        var rawItems = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var items = rawItems.Select(o =>
        {
            var firstItem = o.Items.FirstOrDefault();
            return new OrderSummaryDto
            {
                Id = o.Id,
                CustomerId = o.CustomerId,
                Status = o.Status.ToString(),
                PaymentMethod = o.PaymentMethod,
                PaymentStatus = o.PaymentStatus.ToString(),
                TotalAmount = o.TotalAmount,
                ItemCount = o.Items.Sum(i => i.Quantity),
                FirstItemName = firstItem?.ProductNameSnapshot ?? string.Empty,
                FirstItemImage = firstItem?.ImageUrlSnapshot,
                ShippingFullName = o.ShippingFullName,
                ShippingPhone = o.ShippingPhone,
                ShippingProvider = o.ShippingProvider,
                TrackingNumber = o.TrackingNumber,
                CreatedAt = o.CreatedAt
            };
        }).ToList();

        return new PagedResult<OrderSummaryDto>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalItems = totalItems
        };
    }
}
