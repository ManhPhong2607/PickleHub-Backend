using MediatR;
using PickleHub.Blog.Application.Features.Posts.DTOs;
using PickleHub.Blog.Application.Mappings;
using PickleHub.Blog.Domain.Entities;
using PickleHub.Blog.Domain.Repositories;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;
using PickleHub.Common.ValueObjects;


namespace PickleHub.Blog.Application.Features.Posts.CreatePost
{
    public record CreatePostCommand(
        string Title,
        string Content,
        Guid CategoryId,
        string? Summary,
        string? SeoTitle,
        string? SeoDescription,
        List<Guid>? RelatedProductIds) : IRequest<PostDetailDto>;
    public class CreatePostHandler : IRequestHandler<CreatePostCommand, PostDetailDto>
    {
        private readonly IContentCategoryRepository _categoryRepository;
        private readonly IPostRepository _postRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        public CreatePostHandler(IContentCategoryRepository categoryRepository, IPostRepository postRepository, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
        {
            _categoryRepository = categoryRepository;
            _postRepository = postRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }
        public async Task<PostDetailDto> Handle(CreatePostCommand request, CancellationToken ct)
        {
            var category = await _categoryRepository.GetByIdAsync(request.CategoryId, ct)
                ?? throw new NotFoundException("Không tìm thấy category.");

            var slug = await GenerateUniqueSlugAsync(request.Title, null, ct);
            var post = Post.Create(
                request.Title,
                slug,
                request.Content,
                request.CategoryId,
                _currentUser.UserId,
                request.Summary,
                request.SeoTitle,
                request.SeoDescription
                );
            post.SetRelatedProducts(request.RelatedProductIds);
            _postRepository.Add(post);
            await _unitOfWork.SaveChangesAsync(ct);

            var dto = post.MapToDetailDto();
            dto.CategoryName = category.Name; // post.Category chưa load do vừa mới tạo, gán thủ công
            return dto;
        }

        private async Task<Slug> GenerateUniqueSlugAsync(string name, Guid? excludeId, CancellationToken ct)
        {
            var baseSlug = Slug.Create(name);
            var candidate = baseSlug;
            var counter = 1;

            while (await _postRepository.ExistsBySlugAsync(candidate.Value, excludeId, ct))
                candidate = baseSlug.AppendSuffix(counter++);

            return candidate;
        }
    }
}
