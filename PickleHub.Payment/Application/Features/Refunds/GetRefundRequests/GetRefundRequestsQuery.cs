using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Common.DTOs;
using PickleHub.Payment.Application.Common.Interfaces;
using PickleHub.Payment.Domain.Entities;
using PickleHub.Payment.Domain.Enums;

namespace PickleHub.Payment.Application.Features.Refunds.GetRefundRequests;

public record RefundRequestDto(
    Guid Id,
    Guid PaymentId,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string Reason,
    string? BankCode,
    string? AccountNumber,
    string? AccountName,
    RefundStatus Status,
    string? AdminNote,
    string? BankTransactionReference,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    string? ProcessedBy
);

public record GetRefundRequestsQuery(
    RefundStatus? Status = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<RefundRequestDto>>;

public class GetRefundRequestsQueryHandler(IPaymentDbContext db) 
    : IRequestHandler<GetRefundRequestsQuery, PagedResult<RefundRequestDto>>
{
    public async Task<PagedResult<RefundRequestDto>> Handle(GetRefundRequestsQuery request, CancellationToken ct)
    {
        var query = db.RefundRequests.AsNoTracking();

        if (request.Status.HasValue)
        {
            query = query.Where(r => r.Status == request.Status.Value);
        }

        var totalItems = await query.CountAsync(ct);
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RefundRequestDto(
                r.Id,
                r.PaymentId,
                r.OrderId,
                r.UserId,
                r.Amount,
                r.Reason,
                r.BankCode,
                r.AccountNumber,
                r.AccountName,
                r.Status,
                r.AdminNote,
                r.BankTransactionReference,
                r.CreatedAt,
                r.ProcessedAt,
                r.ProcessedBy
            ))
            .ToListAsync(ct);

        return new PagedResult<RefundRequestDto>
        {
            Items = items,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize
        };
    }
}
