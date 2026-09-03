using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickleHub.Blog.Application.Features.Posts.ArchivePost;
using PickleHub.Blog.Application.Features.Posts.CreatePost;
using PickleHub.Blog.Application.Features.Posts.DeletePost;
using PickleHub.Blog.Application.Features.Posts.GetPosts;
using PickleHub.Blog.Application.Features.Posts.PublishPost;
using PickleHub.Blog.Application.Features.Posts.UpdatePost;
using PickleHub.Blog.Application.Features.Posts.UploadPostImage;

namespace PickleHub.Blog.Controllers
{
    [ApiController]
    [Route("admin/posts")]
    [Authorize(Roles = "Admin")]
    public class AdminPostsController(ISender mediator) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetAdminPostsQuery query, CancellationToken ct)
        {
            var result = await mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var result = await mediator.Send(new GetAdminPostByIdQuery(id), ct);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePostCommand command, CancellationToken ct)
        {
            var result = await mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePostCommand command, CancellationToken ct)
        {
            var result = await mediator.Send(command with { Id = id }, ct);
            return Ok(result);
        }

        [HttpPut("{id:guid}/publish")]
        public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
        {
            await mediator.Send(new PublishPostCommand(id), ct);
            return NoContent();
        }

        [HttpPut("{id:guid}/archive")]
        public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
        {
            await mediator.Send(new ArchivePostCommand(id), ct);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await mediator.Send(new DeletePostCommand(id), ct);
            return NoContent();
        }

        [HttpPost("{id:guid}/cover-image")]
        public async Task<IActionResult> UploadCoverImage(Guid id, IFormFile file, CancellationToken ct)
        {
            var url = await mediator.Send(new UploadPostCoverImageCommand(id, file), ct);
            return Ok(new { url });
        }

        [HttpPost("upload-media")]
        public async Task<IActionResult> UploadInlineMedia(IFormFile file, CancellationToken ct)
        {
            var result = await mediator.Send(new UploadInlineMediaCommand(file), ct);
            return Ok(result);
        }
    }
}

