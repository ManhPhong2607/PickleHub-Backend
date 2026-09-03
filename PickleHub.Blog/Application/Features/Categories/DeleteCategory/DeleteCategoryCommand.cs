using MediatR;
using PickleHub.Blog.Domain.Repositories;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;

namespace PickleHub.Blog.Application.Features.Categories.DeleteCategory
{
    public record DeleteCategoryCommand(Guid Id) : IRequest;

    public class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand>
    {
        private readonly IContentCategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        public DeleteCategoryHandler(IContentCategoryRepository categoryRepository, IUnitOfWork unitOfWork)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task Handle(DeleteCategoryCommand request, CancellationToken ct)
        {
            var category = await _categoryRepository.GetByIdAsync(request.Id, ct)
                ?? throw new NotFoundException("Không tìm thấy category.");

            if(await _categoryRepository.HasPostsAsync(request.Id, ct))
                throw new ConflictException("Không thể xóa category đang có bài viết.");
            _categoryRepository.Remove(category);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
