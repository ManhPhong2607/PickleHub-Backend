using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.CartOrder.Application.Common.Interfaces;
using PickleHub.Common.Enums;

namespace PickleHub.CartOrder.Application.Features.Orders.GetOrderStatusDistribution;

public record GetOrderStatusDistributionQuery(int? Days = 30, DateTime? StartDate = null, DateTime? EndDate = null) : IRequest<OrderStatusDistributionResultDto>;

public class GetOrderStatusDistributionQueryHandler(ICartOrderDbContext db)
    : IRequestHandler<GetOrderStatusDistributionQuery, OrderStatusDistributionResultDto>
{
    public async Task<OrderStatusDistributionResultDto> Handle(GetOrderStatusDistributionQuery request, CancellationToken ct)
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
        var query = db.Orders.Where(o => o.CreatedAt >= startDate && o.CreatedAt < endDate);
        var totalOrders = await query.CountAsync(ct);

        var grouped = await query
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var allStatuses = new[]
        {
            (Status: OrderStatus.Pending, Text: "Chờ xác nhận", Color: "#f59e0b"),   // Amber
            (Status: OrderStatus.Confirmed, Text: "Đã xác nhận", Color: "#a855f7"),  // Purple
            (Status: OrderStatus.Shipping, Text: "Đang giao", Color: "#3b82f6"),     // Blue
            (Status: OrderStatus.Completed, Text: "Hoàn thành", Color: "#10b981"),   // Emerald
            (Status: OrderStatus.Cancelled, Text: "Đã hủy", Color: "#ef4444")        // Red
        };

        var items = allStatuses.Select(s =>
        {
            var matched = grouped.FirstOrDefault(g => g.Status == s.Status);
            var count = matched?.Count ?? 0;
            var pct = totalOrders > 0 ? Math.Round(((decimal)count / totalOrders) * 100m, 1) : 0m;
            return new OrderStatusItemDto
            {
                Status = s.Status.ToString(),
                StatusText = s.Text,
                Count = count,
                Percentage = pct,
                Color = s.Color
            };
        }).ToList();

        return new OrderStatusDistributionResultDto
        {
            Days = totalDays,
            TotalOrders = totalOrders,
            Items = items
        };
    }
}

public class OrderStatusDistributionResultDto
{
    public int Days { get; set; }
    public int TotalOrders { get; set; }
    public List<OrderStatusItemDto> Items { get; set; } = new();
}

public class OrderStatusItemDto
{
    public string Status { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
    public string Color { get; set; } = string.Empty;
}
