using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.CartOrder.Application.Common.Interfaces;
using PickleHub.Common.Enums;

namespace PickleHub.CartOrder.Application.Features.Orders.GetDashboardSummary;

// Query lấy dữ liệu tóm tắt kinh doanh hiển thị trên Admin Dashboard.
public record GetDashboardSummaryQuery : IRequest<OrderDashboardSummaryDto>;

public class GetDashboardSummaryQueryHandler(ICartOrderDbContext db) 
    : IRequestHandler<GetDashboardSummaryQuery, OrderDashboardSummaryDto>
{
    public async Task<OrderDashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var todayOrders = await db.Orders.CountAsync(o => o.CreatedAt >= today, ct);
        var todayRevenue = await db.Orders
            .Where(o => o.CreatedAt >= today && (o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Shipping || o.Status == OrderStatus.Completed))
            .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0m;

        var pendingOrders = await db.Orders.CountAsync(o => o.Status == OrderStatus.Pending, ct);
        var totalOrdersThisMonth = await db.Orders.CountAsync(o => o.CreatedAt >= startOfMonth, ct);

        return new OrderDashboardSummaryDto
        {
            TodayOrders = todayOrders,
            TodayRevenue = todayRevenue,
            PendingOrders = pendingOrders,
            TotalOrdersThisMonth = totalOrdersThisMonth
        };
    }
}

// DTO chứa dữ liệu tóm tắt báo cáo kinh doanh cho Admin Dashboard
public class OrderDashboardSummaryDto
{
    public int TodayOrders { get; set; }
    public decimal TodayRevenue { get; set; }
    public int PendingOrders { get; set; }
    public int TotalOrdersThisMonth { get; set; }
}
