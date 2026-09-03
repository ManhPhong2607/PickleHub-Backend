using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickleHub.Catalog.Application.Features.Products.GetProductVariant;

namespace PickleHub.Catalog.Controllers
{
    [ApiController]
    [Route("products/{productId:guid}/variants")]
    public class ProductVariantsPublicController(ISender mediator) : ControllerBase
    {
        // Lấy tất cả variant của 1 product, dùng để hiện thị trang chi tiết sp
        [HttpGet]
        public async Task<IActionResult> GetVariants(
            Guid productId, CancellationToken ct)
        {
            var result = await mediator.Send(
                new GetProductVariantsQuery(productId), ct);
            return Ok(result);
        }

        // Lấy thông tin 1 variant để snapshot vào cart_item/order_item
        [HttpGet("{variantId:guid}")]
        public async Task<IActionResult> GetVariant(
            Guid productId, Guid variantId, CancellationToken ct)
        {
            var result = await mediator.Send(
                new GetProductVariantQuery(productId, variantId), ct);
            return Ok(result);
        }
    }
}
