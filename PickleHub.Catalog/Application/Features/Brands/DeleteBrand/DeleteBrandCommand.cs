using PickleHub.Common.Interfaces;
using MediatR;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.Exceptions;

namespace PickleHub.Catalog.Application.Features.Brands.DeleteBrand
{
    public record DeleteBrandCommand(Guid Id) : IRequest;
    public class DeleteBrandHandler : IRequestHandler<DeleteBrandCommand>
    {
        private readonly IBrandRepository _brandRepository;
        private readonly IUnitOfWork _unitOfWork;
        public DeleteBrandHandler(IBrandRepository brandRepository, IUnitOfWork unitOfWork)
        {
            _brandRepository = brandRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task Handle(DeleteBrandCommand request, CancellationToken ct)
        {
            var brand = await _brandRepository.GetByIdAsync(request.Id, ct)
                ?? throw new NotFoundException("Thương hiệu không tồn tại.");

            if(await _brandRepository.HasProductsAsync(request.Id, ct))
                throw new ConflictException("Không thể xóa thương hiệu vì có sản phẩm liên quan.");

            _brandRepository.Remove(brand);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
