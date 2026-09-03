using MediatR;
using PickleHub.Blog.Application.Features.Posts.DTOs;
using PickleHub.Blog.Application.Mappings;
using PickleHub.Blog.Domain.Enums;
using PickleHub.Blog.Domain.Repositories;
using PickleHub.Common.Exceptions;

namespace PickleHub.Blog.Application.Features.Posts.GetPosts
{
    public record GetRelatedPostsQuery(string Slug, int Limit = 4) : IRequest<List<PostListDto>>;
    public class GetRelatedPostsHandler : IRequestHandler<GetRelatedPostsQuery, List<PostListDto>>
    {
        private readonly IPostRepository _postRepository;
        public GetRelatedPostsHandler(IPostRepository postRepository)
        {
            _postRepository = postRepository;
        }
        public async Task<List<PostListDto>> Handle(GetRelatedPostsQuery request, CancellationToken ct)
        {
            var post = await _postRepository.GetBySlugAsync(request.Slug, ct)
                ?? throw new NotFoundException("Không tìm thấy bài viết.");

            if (post.Status != PostStatus.Published)
                throw new NotFoundException("Không tìm thấy bài viết.");

            var related = await _postRepository.GetRelatedAsync(post.Id, post.CategoryId, request.Limit, ct);
            return related.Select(p => p.MapToListDto()).ToList();
        }
    }
}
