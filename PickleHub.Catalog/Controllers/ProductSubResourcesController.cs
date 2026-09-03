using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickleHub.Catalog.Application.Features.Products.AddProductImage;
using PickleHub.Catalog.Application.Features.Products.AddProductVarriant;
using PickleHub.Catalog.Application.Features.Products.GetProductVariant;
using PickleHub.Catalog.Application.Features.Products.RemoveImage;
using PickleHub.Catalog.Application.Features.Products.RemoveProductVariant;
using PickleHub.Catalog.Application.Features.Products.ReplaceProductImage;
using PickleHub.Catalog.Application.Features.Products.UpdateProductVariant;

namespace PickleHub.Catalog.Controllers
{
    [ApiController]
    [Route("products/{productId:guid}")]
    [Authorize(Roles = "Admin")]
    public class ProductSubResourcesController(ISender mediator) : ControllerBase
    {
        //Images 

        [HttpPost("images")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddImage(
            Guid productId,
            List<IFormFile> files,
            [FromForm] Guid? variantId = null,
            [FromForm] bool isSizeChart = false,
            CancellationToken ct = default)
        {
            var result = await mediator.Send(new AddProductImageCommand(productId, files, variantId, isSizeChart), ct);
            return Created(string.Empty, result);
        }

        // Thay ảnh tại đúng vị trí hiện có - dùng khi nhóm ảnh đã đạt giới hạn số lượng,.
        [HttpPut("images/{imageId:guid}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ReplaceImage(
            Guid productId,
            Guid imageId,
            IFormFile file,
            CancellationToken ct = default)
        {
            var result = await mediator.Send(new ReplaceProductImageCommand(productId, imageId, file), ct);
            return Ok(result);
        }

        [HttpDelete("images/{imageId:guid}")]
        public async Task<IActionResult> RemoveImage(Guid productId, Guid imageId, CancellationToken ct)
        {
            await mediator.Send(new RemoveProductImageCommand(productId, imageId), ct);
            return NoContent();
        }

        //Variants 
        [HttpPost("variants")]
        public async Task<IActionResult> AddVariant(
            Guid productId,
            [FromBody] AddProductVariantCommand command,
            CancellationToken ct)
        {
            var result = await mediator.Send(command with { ProductId = productId }, ct);
            return Created(string.Empty, result);
        }

        [HttpPut("variants/{variantId:guid}")]
        public async Task<IActionResult> UpdateVariant(
            Guid productId,
            Guid variantId,
            [FromBody] UpdateProductVariantCommand command,
            CancellationToken ct)
        {
            var result = await mediator.Send(command with { ProductId = productId, VariantId = variantId }, ct);
            return Ok(result);
        }

        [HttpDelete("variants/{variantId:guid}")]
        public async Task<IActionResult> RemoveVariant(Guid productId, Guid variantId, CancellationToken ct)
        {
            await mediator.Send(new RemoveProductVariantCommand(productId, variantId), ct);
            return NoContent();
        }
    }
}
