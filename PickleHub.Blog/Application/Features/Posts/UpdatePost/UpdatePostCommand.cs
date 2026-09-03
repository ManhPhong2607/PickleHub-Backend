using MediatR;
using PickleHub.Blog.Application.Features.Posts.DTOs;
using PickleHub.Blog.Application.Mappings;
using PickleHub.Blog.Domain.Repositories;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;
using PickleHub.Common.ValueObjects;

namespace PickleHub.Blog.Application.Features.Posts.UpdatePost
{
    public record UpdatePostCommand(
         Guid Id,
         string Title,
         string Content,
         Guid CategoryId,
         string? Summary,
         string? SeoTitle,
         string? SeoDescription,
         List<Guid>? RelatedProductIds) : IRequest<PostDetailDto>;

    public class UpdatePostHandler : IRequestHandler<UpdatePostCommand, PostDetailDto>
    {
        private readonly IPostRepository _postRepository;
        private readonly IContentCategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdatePostHandler(
            IPostRepository postRepository,
            IContentCategoryRepository categoryRepository,
            IUnitOfWork unitOfWork)
        {
            _postRepository = postRepository;
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<PostDetailDto> Handle(UpdatePostCommand request, CancellationToken ct)
        {
            var post = await _postRepository.GetByIdAsync(request.Id, ct)
                ?? throw new NotFoundException("Không tìm thấy bài viết.");

            var category = await _categoryRepository.GetByIdAsync(request.CategoryId, ct)
                ?? throw new NotFoundException("Không tìm thấy category.");

            var slug = post.PublishedAt.HasValue
                ? post.Slug // đã từng publish -> giữ nguyên slug, tránh vỡ link cũ
                : await GenerateUniqueSlugAsync(request.Title, request.Id, ct);

            post.UpdateContent(
                request.Title,
                slug,
                request.Content,
                request.CategoryId,
                request.Summary,
                request.SeoTitle,
                request.SeoDescription);

            post.SetRelatedProducts(request.RelatedProductIds);

            _postRepository.Update(post);
            await _unitOfWork.SaveChangesAsync(ct);

            var dto = post.MapToDetailDto();
            dto.CategoryName = category.Name;
            return dto;
        }

        private async Task<Slug> GenerateUniqueSlugAsync(string title, Guid excludeId, CancellationToken ct)
        {
            var baseSlug = Slug.Create(title);
            var candidate = baseSlug;
            var counter = 1;

            while (await _postRepository.ExistsBySlugAsync(candidate.Value, excludeId, ct))
                candidate = baseSlug.AppendSuffix(counter++);

            return candidate;
        }
    }
}
