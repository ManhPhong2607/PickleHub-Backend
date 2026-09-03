using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickleHub.Catalog.Application.Features.Promotions.AddProductsToPromotion;
using PickleHub.Catalog.Application.Features.Promotions.CreatePromotion;
using PickleHub.Catalog.Application.Features.Promotions.DeletePromotion;
using PickleHub.Catalog.Application.Features.Promotions.DTOs;
using PickleHub.Catalog.Application.Features.Promotions.GetPromotionById;
using PickleHub.Catalog.Application.Features.Promotions.GetPromotions;
using PickleHub.Catalog.Application.Features.Promotions.RemoveProductFromPromotion;
using PickleHub.Catalog.Application.Features.Promotions.UpdatePromotion;

namespace PickleHub.Catalog.Controllers
{
    [ApiController]
    [Route("admin/promotions")]
    [Authorize(Roles = "Admin")]
    public class PromotionsController(ISender mediator) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetPromotionsQuery query, CancellationToken ct)
        {
            var result = await mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var result = await mediator.Send(new GetPromotionByIdQuery(id), ct);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePromotionCommand command, CancellationToken ct)
        {
            var result = await mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Promotion.Id }, result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePromotionCommand command, CancellationToken ct)
        {       
            var result = await mediator.Send(command with { PromotionId = id}, ct);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await mediator.Send(new DeletePromotionCommand(id), ct);
            return NoContent();
        }

        //gán thêm sản phẩm vào promoton đã có sẵn
        [HttpPost("{id:guid}/products")]
        public async Task<IActionResult> AddProducts(Guid id, [FromBody] List<PromotionItemInput> items, CancellationToken ct)
        {
            var result = await mediator.Send(new AddProductsToPromotionCommand(id, items), ct);
            return Ok(result);
        }

        [HttpDelete("{id:guid}/products")]
        public async Task<IActionResult> RemoveProducts(Guid id, [FromBody] List<Guid> productIds,  CancellationToken ct)
        {
            await mediator.Send( new RemoveProductFromPromotionCommand(id, productIds),ct);
            return NoContent();
        }
    }
}
