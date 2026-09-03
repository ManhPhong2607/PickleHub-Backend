using MediatR;
using PickleHub.Blog.Application.Features.Posts.DTOs;
using PickleHub.Blog.Application.Mappings;
using PickleHub.Blog.Domain.Enums;
using PickleHub.Blog.Domain.Repositories;
using PickleHub.Common.DTOs;

namespace PickleHub.Blog.Application.Features.Posts.GetPosts
{
    // Admin list — mọi status
    public record GetAdminPostsQuery(string? Keyword, Guid? CategoryId, PostStatus? Status, int Page = 1, int PageSize = 20)
        : IRequest<PagedResult<AdminPostListDto>>;

    public class GetAdminPostsHandler : IRequestHandler<GetAdminPostsQuery, PagedResult<AdminPostListDto>>
    {
        private readonly IPostRepository _postRepository;

        public GetAdminPostsHandler(IPostRepository postRepository)
        {
            _postRepository = postRepository;
        }

        public async Task<PagedResult<AdminPostListDto>> Handle(GetAdminPostsQuery request, CancellationToken ct)
        {
            var (items, total) = await _postRepository.GetAdminPagedAsync(
                request.Keyword, request.CategoryId, request.Status, request.Page, request.PageSize, ct);

            return new PagedResult<AdminPostListDto>
            {
                Items = items.Select(p => p.MapToAdminListDto()).ToList(),
                Page = request.Page,
                PageSize = request.PageSize,
                TotalItems = total
            };
        }
    }
}