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
        var yesterday = today.AddDays(-1);
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfLastMonth = startOfMonth.AddMonths(-1);

        var todayOrders = await db.Orders.CountAsync(o => o.CreatedAt >= today, ct);
        var todayRevenue = await db.Orders
            .Where(o => o.CreatedAt >= today && (o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Shipping || o.Status == OrderStatus.Completed))
            .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0m;

        var yesterdayOrders = await db.Orders.CountAsync(o => o.CreatedAt >= yesterday && o.CreatedAt < today, ct);
        var yesterdayRevenue = await db.Orders
            .Where(o => o.CreatedAt >= yesterday && o.CreatedAt < today && (o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Shipping || o.Status == OrderStatus.Completed))
            .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0m;

        decimal revenueGrowth = 0m;
        if (yesterdayRevenue > 0)
            revenueGrowth = Math.Round(((todayRevenue - yesterdayRevenue) / yesterdayRevenue) * 100m, 1);
        else if (todayRevenue > 0)
            revenueGrowth = 100m;

        decimal ordersGrowth = 0m;
        if (yesterdayOrders > 0)
            ordersGrowth = Math.Round((((decimal)todayOrders - yesterdayOrders) / yesterdayOrders) * 100m, 1);
        else if (todayOrders > 0)
            ordersGrowth = 100m;

        var pendingOrders = await db.Orders.CountAsync(o => o.Status == OrderStatus.Pending, ct);
        var totalOrdersThisMonth = await db.Orders.CountAsync(o => o.CreatedAt >= startOfMonth, ct);

        return new OrderDashboardSummaryDto
        {
            TodayOrders = todayOrders,
            TodayRevenue = todayRevenue,
            YesterdayOrders = yesterdayOrders,
            YesterdayRevenue = yesterdayRevenue,
            RevenueGrowthPercent = revenueGrowth,
            OrdersGrowthPercent = ordersGrowth,
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
    public int YesterdayOrders { get; set; }
    public decimal YesterdayRevenue { get; set; }
    public decimal RevenueGrowthPercent { get; set; }
    public decimal OrdersGrowthPercent { get; set; }
    public int PendingOrders { get; set; }
    public int TotalOrdersThisMonth { get; set; }
}
