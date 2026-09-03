using MediatR;
using Microsoft.AspNetCore.Mvc;
using PickleHub.Blog.Application.Features.Posts.GetPosts;

namespace PickleHub.Blog.Controllers
{
    [ApiController]
    [Route("posts")]
    public class PostsController(ISender mediator) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetPostsQuery query, CancellationToken ct)
        {
            var result = await mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
        {
            var result = await mediator.Send(new GetPostBySlugQuery(slug), ct);
            return Ok(result);
        }

        [HttpGet("{slug}/related")]
        public async Task<IActionResult> GetRelated(string slug, [FromQuery] int limit = 4, CancellationToken ct = default)
        {
            var result = await mediator.Send(new GetRelatedPostsQuery(slug, limit), ct);
            return Ok(result);
        }
    }
}
