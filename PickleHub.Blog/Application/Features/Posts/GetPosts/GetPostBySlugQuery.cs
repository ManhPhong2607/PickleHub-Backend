using MediatR;
using PickleHub.Blog.Application.Common.Interfaces;
using PickleHub.Blog.Application.Features.Posts.DTOs;
using PickleHub.Blog.Application.Mappings;
using PickleHub.Blog.Domain.Enums;
using PickleHub.Blog.Domain.Repositories;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;

namespace PickleHub.Blog.Application.Features.Posts.GetPosts
{
    // Dùng cho public — tăng ViewCount, kèm related products + previous/next
    public record GetPostBySlugQuery(string Slug) : IRequest<PostDetailDto>;

    public class GetPostBySlugHandler : IRequestHandler<GetPostBySlugQuery, PostDetailDto>
    {
        private readonly IPostRepository _postRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICatalogClient _catalogClient;

        public GetPostBySlugHandler(
            IPostRepository postRepository,
            IUnitOfWork unitOfWork,
            ICatalogClient catalogClient)
        {
            _postRepository = postRepository;
            _unitOfWork = unitOfWork;
            _catalogClient = catalogClient;
        }

        public async Task<PostDetailDto> Handle(GetPostBySlugQuery request, CancellationToken ct)
        {
            var post = await _postRepository.GetBySlugAsync(request.Slug, ct)
                ?? throw new NotFoundException("Không tìm thấy bài viết.");

            if (post.Status != PostStatus.Published)
                throw new NotFoundException("Không tìm thấy bài viết.");

            post.IncreaseViewCount();
            await _unitOfWork.SaveChangesAsync(ct);

            var dto = post.MapToDetailDto();

            var previous = await _postRepository.GetPreviousPublishedAsync(post.CategoryId, post.PublishedAt!.Value, ct);
            var next = await _postRepository.GetNextPublishedAsync(post.CategoryId, post.PublishedAt!.Value, ct);

            dto.PreviousPost = previous is null ? null : new AdjacentPostDto
            {
                Id = previous.Id,
                Title = previous.Title,
                Slug = previous.Slug.Value
            };

            dto.NextPost = next is null ? null : new AdjacentPostDto
            {
                Id = next.Id,
                Title = next.Title,
                Slug = next.Slug.Value
            };

            if (post.RelatedProductIds is { Count: > 0 })
                dto.RelatedProducts = await _catalogClient.GetProductsByIdsAsync(post.RelatedProductIds, ct);

            return dto;
        }
    }
}
