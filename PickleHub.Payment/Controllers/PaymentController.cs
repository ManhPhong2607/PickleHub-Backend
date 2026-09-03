using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayOS.Models.Webhooks;
using PickleHub.Payment.Application.Features.Payments.CreatePayment;
using PickleHub.Payment.Application.Features.Payments.HandleWebhook;
using PickleHub.Payment.Application.Features.Payments.GetPaymentStatus;

using Microsoft.Extensions.Configuration;

namespace PickleHub.Payment.Controllers;

[ApiController]
[Route("payments")]
[Authorize]
public class PaymentController(ISender mediator, IConfiguration config) : ControllerBase
{
    // POST /payments/create-link -> Tạo link thanh toán QR Code qua PayOS
    [HttpPost("create-link")]
    public async Task<IActionResult> CreatePaymentLink(
        [FromBody] CreatePaymentRequest request, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new CreatePaymentCommand(request.OrderId, request.Amount), ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Lỗi khi tạo yêu cầu thanh toán.", error = ex.Message });
        }
    }

    // POST /internal/payments/create-link -> Endpoint nội bộ cho CartOrder Service gọi sang (Bảo mật qua X-Internal-Key)
    [HttpPost("~/internal/payments/create-link")]
    [AllowAnonymous]
    public async Task<IActionResult> CreatePaymentLinkInternal(
        [FromBody] CreatePaymentRequest request, CancellationToken ct)
    {
        var internalToken = config["Security:InternalApiKey"]
            ?? throw new InvalidOperationException("Thiếu cấu hình Security:InternalApiKey");
        if (!Request.Headers.TryGetValue("X-Internal-Key", out var headerKey) || headerKey != internalToken)
        {
            return Unauthorized("Yêu cầu này không hợp lệ hoặc thiếu mã khóa dịch vụ nội bộ.");
        }

        try
        {
            var result = await mediator.Send(new CreatePaymentCommand(request.OrderId, request.Amount), ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Lỗi khi tạo yêu cầu thanh toán nội bộ.", error = ex.Message });
        }
    }

    // GET /payments/status/{orderId:guid} -> Lấy trạng thái thanh toán thực tế của đơn hàng (cho Frontend tự check/verify)
    [HttpGet("status/{orderId:guid}")]
    public async Task<ActionResult<PaymentStatusDto>> GetPaymentStatus(Guid orderId, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetPaymentStatusQuery(orderId), ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Lỗi khi kiểm tra trạng thái thanh toán.", error = ex.Message });
        }
    }

    // POST /payments/webhook -> Tiếp nhận kết quả thanh toán tự động từ PayOS (gọi không cần token, tự verify chữ ký số)
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> HandlePayOsWebhook([FromBody] Webhook webhookBody, CancellationToken ct)
    {
        try
        {
            var success = await mediator.Send(new HandleWebhookCommand(webhookBody), ct);
            
            if (success)
            {
                return Ok(new { success = true });
            }
            
            return BadRequest(new { message = "Xử lý thông tin đối soát Webhook thất bại." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Xác thực chữ ký số Webhook thất bại hoặc giao dịch không hợp lệ.", error = ex.Message });
        }
    }

    // GET /payments/refunds -> Danh sách yêu cầu hoàn tiền cho Admin
    [HttpGet("refunds")]
    public async Task<IActionResult> GetRefundRequests(
        [FromQuery] Domain.Enums.RefundStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var result = await mediator.Send(new Application.Features.Refunds.GetRefundRequests.GetRefundRequestsQuery(status, page, pageSize), ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Lỗi khi lấy danh sách yêu cầu hoàn tiền.", error = ex.Message });
        }
    }

    // GET /payments/refunds/order/{orderId:guid} -> Lấy thông tin hoàn tiền theo đơn hàng
    [HttpGet("refunds/order/{orderId:guid}")]
    public async Task<IActionResult> GetRefundByOrder(Guid orderId, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new Application.Features.Refunds.GetRefundByOrder.GetRefundByOrderQuery(orderId), ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Lỗi khi tra cứu yêu cầu hoàn tiền của đơn hàng.", error = ex.Message });
        }
    }

    // PUT /payments/refunds/order/{orderId:guid}/bank-info -> Cập nhật thông tin STK ngân hàng nhận tiền hoàn
    [HttpPut("refunds/order/{orderId:guid}/bank-info")]
    public async Task<IActionResult> UpdateRefundBankInfo(
        Guid orderId,
        [FromBody] UpdateRefundBankInfoRequest request,
        CancellationToken ct)
    {
        try
        {
            var userId = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : (Guid?)null;
            var success = await mediator.Send(new Application.Features.Refunds.UpdateRefundBankInfo.UpdateRefundBankInfoCommand(
                orderId,
                request.BankCode,
                request.AccountNumber,
                request.AccountName,
                userId
            ), ct);

            return Ok(new { success, message = "Cập nhật thông tin tài khoản nhận tiền hoàn thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Không thể cập nhật thông tin tài khoản hoàn tiền.", error = ex.Message });
        }
    }

    // POST /payments/refunds/{id:guid}/process -> Admin duyệt hoặc từ chối hoàn tiền
    [HttpPost("refunds/{id:guid}/process")]
    public async Task<IActionResult> ProcessRefund(
        Guid id,
        [FromBody] ProcessRefundRequest request,
        CancellationToken ct)
    {
        try
        {
            var userName = User.Identity?.Name ?? "Admin";
            var success = await mediator.Send(new Application.Features.Refunds.ProcessRefund.ProcessRefundCommand(
                id,
                request.Action,
                request.BankTransactionReference,
                request.AdminNote,
                userName
            ), ct);

            return Ok(new { success, message = request.Action == "Approve" ? "Đã duyệt hoàn tiền thành công." : "Đã từ chối yêu cầu hoàn tiền." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Xử lý yêu cầu hoàn tiền thất bại.", error = ex.Message });
        }
    }
}

// DTO Requests
public record CreatePaymentRequest(Guid OrderId, decimal Amount);
public record UpdateRefundBankInfoRequest(string BankCode, string AccountNumber, string AccountName);
public record ProcessRefundRequest(string Action, string? BankTransactionReference, string? AdminNote);

