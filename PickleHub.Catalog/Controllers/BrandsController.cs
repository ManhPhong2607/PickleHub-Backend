using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickleHub.Catalog.Application.Features.Brands.CreateBrand;
using PickleHub.Catalog.Application.Features.Brands.DeleteBrand;
using PickleHub.Catalog.Application.Features.Brands.GetBrand;
using PickleHub.Catalog.Application.Features.Brands.UpdateBrand;

namespace PickleHub.Catalog.Controllers
{
    [ApiController]
    [Route("brands")]
    public class BrandsController(ISender mediator) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var result = await mediator.Send(new GetBrandsQuery(), ct);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateBrandCommand command, CancellationToken ct)
        {
            var result = await mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetAll), result);
        }


        [HttpPut("{brandId:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid brandId, [FromBody] UpdateBrandCommand command, CancellationToken ct)
        {
            var result = await mediator.Send(command with { BrandId = brandId }, ct);
            return Ok(result);
        }

        [HttpDelete("{brandId:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid brandId, CancellationToken ct)
        {
            await mediator.Send(new DeleteBrandCommand(brandId), ct);
            return NoContent();

        }
    }
}
