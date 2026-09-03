using MediatR;
using PickleHub.Blog.Application.Common.Interfaces;
using PickleHub.Blog.Application.Features.Posts.DTOs;
using PickleHub.Blog.Application.Mappings;
using PickleHub.Blog.Domain.Repositories;
using PickleHub.Common.Exceptions;

namespace PickleHub.Blog.Application.Features.Posts.GetPosts
{
    // Dùng cho admin form edit — KHÔNG tăng ViewCount
    public record GetAdminPostByIdQuery(Guid Id) : IRequest<PostDetailDto>;

    public class GetAdminPostByIdHandler : IRequestHandler<GetAdminPostByIdQuery, PostDetailDto>
    {
        private readonly IPostRepository _postRepository;
        private readonly ICatalogClient _catalogClient;

        public GetAdminPostByIdHandler(IPostRepository postRepository, ICatalogClient catalogClient)
        {
            _postRepository = postRepository;
            _catalogClient = catalogClient;
        }

        public async Task<PostDetailDto> Handle(GetAdminPostByIdQuery request, CancellationToken ct)
        {
            var post = await _postRepository.GetByIdAsync(request.Id, ct)
                ?? throw new NotFoundException("Không tìm thấy bài viết.");

            var dto = post.MapToDetailDto();

            if (post.RelatedProductIds?.Count > 0 )
                dto.RelatedProducts = await _catalogClient.GetProductsByIdsAsync(post.RelatedProductIds, ct);

            return dto;
        }
    }
}
