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
public class PaymentController(ISender mediator) : ControllerBase
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
}

// DTO Requests
public record CreatePaymentRequest(Guid OrderId, decimal Amount);
