using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickleHub.Catalog.Application.Features.Categories.CreateCategory;
using PickleHub.Catalog.Application.Features.Categories.DeleteCategory;
using PickleHub.Catalog.Application.Features.Categories.GetCategory;
using PickleHub.Catalog.Application.Features.Categories.RemoveCategoryImage;
using PickleHub.Catalog.Application.Features.Categories.UpdateCategory;
using PickleHub.Catalog.Application.Features.Categories.UpdateCategoryAttributeSchema;
using PickleHub.Catalog.Application.Features.Categories.UploadCategoryImage;

namespace PickleHub.Catalog.Controllers
{
    [ApiController]
    [Route("categories")]
    public class CategoriesController(ISender mediator) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetCategoriesQuery query, CancellationToken ct)
        {
            var result = await mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateCategoryCommand command, CancellationToken ct)
        {
            var result = await mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetAll), result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryCommand command, CancellationToken ct)
        {
            var result = await mediator.Send(command with { Id = id }, ct);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await mediator.Send(new DeleteCategoryCommand(id), ct);
            return NoContent();
        }

        [HttpPost("{id:guid}/image")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage(Guid id, [FromForm] IFormFile file, CancellationToken ct)
        {
            var result = await mediator.Send(new UploadCategoryImageCommand(id, file), ct);
            return Ok(result);
        }

        [HttpDelete("{id:guid}/image")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveImage(Guid id, CancellationToken ct)
        {
            var result = await mediator.Send(new RemoveCategoryImageCommand(id), ct);
            return Ok(result);
        }

        [HttpPut("{id:guid}/attribute-schema")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAttributeSchema(Guid id, [FromBody] UpdateCategoryAttributeSchemaCommand command, CancellationToken ct)
        {
            var result = await mediator.Send(command with { Id = id }, ct);
            return Ok(result);
        }
    }
}
