using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickleHub.Blog.Application.Features.Categories.CreateCategory;
using PickleHub.Blog.Application.Features.Categories.DeleteCategory;
using PickleHub.Blog.Application.Features.Categories.GetCategory;
using PickleHub.Blog.Application.Features.Categories.UpdateCategory;

namespace PickleHub.Blog.Controllers
{
    [ApiController]
    [Route("post-categories")]
    public class CategoriesController(ISender mediator) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var result = await mediator.Send(new GetCategoriesQuery(), ct);
            return Ok(result);
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
        {
            var result = await mediator.Send(new GetCategoryBySlugQuery(slug), ct);
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
    }
}
