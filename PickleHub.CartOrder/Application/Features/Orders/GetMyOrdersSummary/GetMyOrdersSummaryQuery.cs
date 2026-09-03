using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.CartOrder.Application.Common.Interfaces;
using PickleHub.Common.Enums;

namespace PickleHub.CartOrder.Application.Features.Orders.GetMyOrdersSummary;

public record OrderBadgeSummaryDto(
    int ChoXacNhan,
    int DaXacNhan,
    int DangGiao,
    int ChoDanhGia
);

// Query lấy số lượng đơn hàng theo từng trạng thái để hiển thị badge số trên Customer Panel (Endpoint 9).
public record GetMyOrdersSummaryQuery(Guid CustomerId) : IRequest<OrderBadgeSummaryDto>;

public class GetMyOrdersSummaryQueryHandler(ICartOrderDbContext db)
    : IRequestHandler<GetMyOrdersSummaryQuery, OrderBadgeSummaryDto>
{
    public async Task<OrderBadgeSummaryDto> Handle(GetMyOrdersSummaryQuery request, CancellationToken ct)
    {
        var orders = await db.Orders
            .Where(o => o.CustomerId == request.CustomerId)
            .ToListAsync(ct);

        int choXacNhan = orders.Count(o => o.Status == OrderStatus.Pending);
        int daXacNhan = orders.Count(o => o.Status == OrderStatus.Confirmed);
        int dangGiao = orders.Count(o => o.Status == OrderStatus.Shipping);
        
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        int choDanhGia = orders.Count(o => o.Status == OrderStatus.Completed && o.CreatedAt >= thirtyDaysAgo);

        return new OrderBadgeSummaryDto(
            choXacNhan,
            daXacNhan,
            dangGiao,
            choDanhGia
        );
    }
}
