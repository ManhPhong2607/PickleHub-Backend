using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickleHub.Review.Application.DTOs;
using PickleHub.Review.Application.Features.AdminModeration;
using PickleHub.Review.Application.Features.AdminReply;
using PickleHub.Review.Application.Features.CreateReview;
using PickleHub.Review.Application.Features.DeleteReview;
using PickleHub.Review.Application.Features.GetAdminReviews;
using PickleHub.Review.Application.Features.GetProductRatingSummary;
using PickleHub.Review.Application.Features.GetProductReviews;
using PickleHub.Review.Application.Features.ToggleLikeReview;
using PickleHub.Review.Application.Features.UpdateReview;

namespace PickleHub.Review.Controllers;

// Controller xử lý tất cả các API Endpoints liên quan đến Đánh giá Sản phẩm (Product Reviews)
[ApiController]
[Route("api/reviews")]
[Route("reviews")]
public class ReviewController(ISender mediator) : ControllerBase
{
    private Guid? GetCurrentUserId()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdStr, out var userId) ? userId : null;
    }

    // API Lấy danh sách toàn bộ đánh giá cho Quản trị viên (Admin Review Moderation & Reply)
    [HttpGet("/admin/reviews")]
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminReviews([FromQuery] GetAdminReviewsQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return Ok(result);
    }
    
    // API Lấy Cloudinary Signed Upload Signature bảo mật cho Review
    [HttpGet("upload-signature")]
    [Authorize]
    public async Task<IActionResult> GetUploadSignature(
        [FromQuery] Guid orderId,
        [FromQuery] Guid productId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var query = new PickleHub.Review.Application.Features.GetCloudinarySignature.GetCloudinarySignatureQuery(userId.Value, orderId, productId);
        var result = await mediator.Send(query);
        return Ok(result);
    }

    // API Tạo mới bài đánh giá sản phẩm (Bắt buộc OrderId theo Rule FR-26)
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto dto)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var command = new CreateReviewCommand(
            userId.Value,
            dto.ProductId,
            dto.OrderId,
            dto.ProductVariantId,
            dto.Rating,
            dto.Comment,
            dto.ImageUrls
        );

        var result = await mediator.Send(command);
        return CreatedAtAction(nameof(GetProductReviews), new { productId = dto.ProductId }, result);
    }
    
    // API Cập nhật/Chỉnh sửa bài đánh giá đã gửi (Yêu cầu chính chủ)
    [HttpPut("{reviewId:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateReview(Guid reviewId, [FromBody] UpdateReviewDto dto)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var command = new UpdateReviewCommand(
            reviewId,
            userId.Value,
            dto.Rating,
            dto.Comment,
            dto.ImageUrls
        );

        var result = await mediator.Send(command);
        return Ok(result);
    }


    // API Lấy danh sách bài đánh giá cá nhân của User đang đăng nhập
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyReviews()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var query = new PickleHub.Review.Application.Features.GetMyReviews.GetMyReviewsQuery(userId.Value);
        var result = await mediator.Send(query);
        return Ok(result);
    }

    // API Lấy danh sách đánh giá của sản phẩm (Hỗ trợ phân trang & bộ lọc)
    [HttpGet("product/{productId:guid}")]
    public async Task<IActionResult> GetProductReviews(
        Guid productId,
        [FromQuery] int? rating,
        [FromQuery] bool? hasImages,
        [FromQuery] bool? verifiedOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var currentUserId = GetCurrentUserId();
        var query = new GetProductReviewsQuery(productId, rating, hasImages, verifiedOnly, currentUserId, page, pageSize);
        var result = await mediator.Send(query);
        return Ok(result);
    }


// API Lấy thông tin tổng quan điểm đánh giá của sản phẩm (Average rating, số đếm mốc 1-5 sao)

    [HttpGet("product/{productId:guid}/summary")]
    public async Task<IActionResult> GetProductRatingSummary(Guid productId)
    {
        var query = new GetProductRatingSummaryQuery(productId);
        var result = await mediator.Send(query);
        return Ok(result);
    }


// API Bấm Like / Bỏ Like cho nút "Hữu ích" của bài đánh giá (Yêu cầu đăng nhập)

    [HttpPost("{reviewId:guid}/like")]
    [Authorize]
    public async Task<IActionResult> ToggleLike(Guid reviewId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var command = new ToggleLikeReviewCommand(reviewId, userId.Value);
        var isLiked = await mediator.Send(command);
        return Ok(new { reviewId, isLiked });
    }


// API Cho phép Admin/Seller đăng phản hồi câu trả lời (Yêu cầu quyền Admin)

    [HttpPatch("{reviewId:guid}/reply")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminReply(Guid reviewId, [FromBody] SellerReplyDto dto)
    {
        var command = new AdminReplyReviewCommand(reviewId, dto.Reply);
        var result = await mediator.Send(command);
        return Ok(result);
    }


// API Cho phép Admin Ẩn / Bỏ ẩn bài đánh giá vi phạm quy định (Yêu cầu quyền Admin)

    [HttpPatch("{reviewId:guid}/hide")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> HideReview(Guid reviewId, [FromBody] HideReviewRequest request)
    {
        var command = new HideReviewCommand(reviewId, request.IsHidden, request.Reason);
        await mediator.Send(command);
        return Ok();
    }


// API Xóa bài đánh giá (Yêu cầu chính chủ hoặc quyền Admin)

    [HttpDelete("{reviewId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteReview(Guid reviewId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        bool isAdmin = User.IsInRole("Admin");
        var command = new DeleteReviewCommand(reviewId, userId.Value, isAdmin);
        await mediator.Send(command);

        return NoContent();
    }
}

public record HideReviewRequest(bool IsHidden, string? Reason = null);
