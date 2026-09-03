using PickleHub.Common.Interfaces;
using MediatR;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.Exceptions;

namespace PickleHub.Catalog.Application.Features.Categories.DeleteCategory
{
    public record DeleteCategoryCommand(Guid Id) : IRequest;

    public class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteCategoryHandler(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteCategoryCommand request, CancellationToken ct)
        {
            var category = await _categoryRepository.GetByIdAsync(request.Id, ct)
                ?? throw new NotFoundException("Danh mục không tồn tại.");

            if (await _categoryRepository.HasChildrenAsync(request.Id, ct)
                || await _categoryRepository.HasProductsAsync(request.Id, ct))
                throw new ConflictException("Danh mục vẫn còn sản phẩm hoặc danh mục con, không thể xóa.");

            _categoryRepository.Remove(category);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
