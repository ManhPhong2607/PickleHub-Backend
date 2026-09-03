using MediatR;
using PickleHub.Blog.Application.Features.Posts.DTOs;
using PickleHub.Blog.Application.Mappings;
using PickleHub.Blog.Domain.Repositories;
using PickleHub.Common.DTOs;

namespace PickleHub.Blog.Application.Features.Posts.GetPosts
{
    // list bài published — filter theo categorySlug thay vì categoryId (thân thiện với public API)
    public record GetPostsQuery(string? Keyword, string? CategorySlug, int Page = 1, int PageSize = 12)
        : IRequest<PagedResult<PostListDto>>;

    public class GetPostsHandler : IRequestHandler<GetPostsQuery, PagedResult<PostListDto>>
    {
        private readonly IPostRepository _postRepository;
        private readonly IContentCategoryRepository _categoryRepository;

        public GetPostsHandler(IPostRepository postRepository, IContentCategoryRepository categoryRepository)
        {
            _postRepository = postRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task<PagedResult<PostListDto>> Handle(GetPostsQuery request, CancellationToken ct)
        {
            Guid? categoryId = null;

            if (!string.IsNullOrWhiteSpace(request.CategorySlug))
            {
                var category = await _categoryRepository.GetBySlugAsync(request.CategorySlug, ct);
                if (category is null)
                    return new PagedResult<PostListDto>
                    {
                        Items = [],
                        Page = request.Page,
                        PageSize = request.PageSize,
                        TotalItems = 0
                    };

                categoryId = category.Id;
            }

            var (items, total) = await _postRepository.GetPublishedPagedAsync(
                request.Keyword, categoryId, request.Page, request.PageSize, ct);

            return new PagedResult<PostListDto>
            {
                Items = items.Select(p => p.MapToListDto()).ToList(),
                Page = request.Page,
                PageSize = request.PageSize,
                TotalItems = total
            };
        }
    }
}
