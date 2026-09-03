using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.CartOrder.Application.Common.Interfaces;
using PickleHub.Common.Enums;

namespace PickleHub.CartOrder.Application.Features.Orders.GetRevenueAnalytics;

public record GetRevenueAnalyticsQuery(int? Days = 30, DateTime? StartDate = null, DateTime? EndDate = null) : IRequest<RevenueAnalyticsResultDto>;

public class GetRevenueAnalyticsQueryHandler(ICartOrderDbContext db)
    : IRequestHandler<GetRevenueAnalyticsQuery, RevenueAnalyticsResultDto>
{
    public async Task<RevenueAnalyticsResultDto> Handle(GetRevenueAnalyticsQuery request, CancellationToken ct)
    {
        DateTime startDate;
        DateTime endDate;

        if (request.StartDate.HasValue && request.EndDate.HasValue)
        {
            var s = request.StartDate.Value;
            var e = request.EndDate.Value;
            // Ép kiểu DateTimeKind.Utc để tương thích 100% với PostgreSQL/Npgsql và EF Core
            startDate = DateTime.SpecifyKind(s.Date, DateTimeKind.Utc);
            endDate = DateTime.SpecifyKind(e.Date.AddDays(1), DateTimeKind.Utc); // Bao gồm trọn vẹn ngày kết thúc
        }
        else
        {
            var days = request.Days.GetValueOrDefault(30);
            if (days <= 0) days = 30;
            var today = DateTime.UtcNow.Date;
            startDate = DateTime.SpecifyKind(today.AddDays(-days + 1), DateTimeKind.Utc);
            endDate = DateTime.SpecifyKind(today.AddDays(1), DateTimeKind.Utc);
        }

        var totalDays = (int)Math.Max(1, (endDate - startDate).TotalDays);
        var prevStartDate = startDate.AddDays(-totalDays);
        var prevEndDate = startDate;

        // Order hiện tại (k tính đơn bị hủy)
        var currentOrders = await db.Orders
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt < endDate && o.Status != OrderStatus.Cancelled)
            .Select(o => new { o.CreatedAt, o.TotalAmount })
            .ToListAsync(ct);

        // Order của kỳ trước (để so sánh)
        var prevOrders = await db.Orders
            .Where(o => o.CreatedAt >= prevStartDate && o.CreatedAt < prevEndDate && o.Status != OrderStatus.Cancelled)
            .Select(o => new { o.CreatedAt, o.TotalAmount })
            .ToListAsync(ct);

        var currentTotalRevenue = currentOrders.Sum(o => o.TotalAmount);
        var currentTotalOrders = currentOrders.Count;

        var prevTotalRevenue = prevOrders.Sum(o => o.TotalAmount);
        var prevTotalOrders = prevOrders.Count;

        decimal revenueGrowth = 0m;
        if (prevTotalRevenue > 0)
        {
            revenueGrowth = Math.Round(((currentTotalRevenue - prevTotalRevenue) / prevTotalRevenue) * 100m, 1);
        }
        else if (currentTotalRevenue > 0)
        {
            revenueGrowth = 100m;
        }

        decimal ordersGrowth = 0m;
        if (prevTotalOrders > 0)
        {
            ordersGrowth = Math.Round((((decimal)currentTotalOrders - prevTotalOrders) / prevTotalOrders) * 100m, 1);
        }
        else if (currentTotalOrders > 0)
        {
            ordersGrowth = 100m;
        }

        // Group order theo ngày
        var ordersByDate = currentOrders
            .GroupBy(o => o.CreatedAt.Date)
            .ToDictionary(
                g => g.Key,
                g => new { Revenue = g.Sum(x => x.TotalAmount), Count = g.Count() }
            );

        // Timeline tất cả các ngày trong khoảng
        var timeline = new List<DailyRevenuePointDto>();
        var lastDay = endDate.AddDays(-1);
        for (var d = startDate; d <= lastDay; d = d.AddDays(1))
        {
            var dKey = d.Date;
            if (ordersByDate.TryGetValue(dKey, out var val))
            {
                timeline.Add(new DailyRevenuePointDto
                {
                    Date = d.ToString("yyyy-MM-dd"),
                    FormattedDate = d.ToString("dd/MM"),
                    Revenue = val.Revenue,
                    OrderCount = val.Count
                });
            }
            else
            {
                timeline.Add(new DailyRevenuePointDto
                {
                    Date = d.ToString("yyyy-MM-dd"),
                    FormattedDate = d.ToString("dd/MM"),
                    Revenue = 0m,
                    OrderCount = 0
                });
            }
        }

        return new RevenueAnalyticsResultDto
        {
            Days = totalDays,
            TotalRevenue = currentTotalRevenue,
            TotalOrders = currentTotalOrders,
            PreviousPeriodRevenue = prevTotalRevenue,
            PreviousPeriodOrders = prevTotalOrders,
            RevenueGrowthPercent = revenueGrowth,
            OrdersGrowthPercent = ordersGrowth,
            Timeline = timeline
        };
    }
}

public class RevenueAnalyticsResultDto
{
    public int Days { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public decimal PreviousPeriodRevenue { get; set; }
    public int PreviousPeriodOrders { get; set; }
    public decimal RevenueGrowthPercent { get; set; }
    public decimal OrdersGrowthPercent { get; set; }
    public List<DailyRevenuePointDto> Timeline { get; set; } = new();
}

public class DailyRevenuePointDto
{
    public string Date { get; set; } = string.Empty;
    public string FormattedDate { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}
