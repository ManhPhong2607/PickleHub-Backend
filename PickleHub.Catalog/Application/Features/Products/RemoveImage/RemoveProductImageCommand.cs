using MediatR;
using Microsoft.Extensions.Logging;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;

namespace PickleHub.Catalog.Application.Features.Products.RemoveImage
{
    public record RemoveProductImageCommand(Guid ProductId, Guid ImageId) : IRequest;

    public class RemoveProductImageHandler : IRequestHandler<RemoveProductImageCommand>
    {
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStorageService _storage;
        private readonly ILogger<RemoveProductImageHandler> _logger;

        public RemoveProductImageHandler(
            IProductRepository productRepository,
            IUnitOfWork unitOfWork,
            IStorageService storage,
            ILogger<RemoveProductImageHandler> logger)
        {
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
            _storage = storage;
            _logger = logger;
        }

        public async Task Handle(RemoveProductImageCommand request, CancellationToken ct)
        {
            var product = await _productRepository.GetByIdWithDetailAsync(request.ProductId, ct)
                ?? throw new NotFoundException("Sản phẩm không tồn tại.");

            var image = product.Images.FirstOrDefault(i => i.Id == request.ImageId)
                ?? throw new NotFoundException("Ảnh không tồn tại trong sản phẩm này.");

            var publicId = image.PublicId;

            product.RemoveImage(request.ImageId);
            await _unitOfWork.SaveChangesAsync(ct);

            // DB đã commit → xóa file trên Cloudinary
            // Nếu fail ở đây: DB sạch (record đã xóa), chỉ để lại file rác trên Cloudinary
            if (!string.IsNullOrEmpty(publicId))
            {
                try
                {
                    await _storage.DeleteAsync(publicId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Không thể xóa ảnh {PublicId} khỏi Cloudinary (productId={ProductId}, imageId={ImageId}). File có thể bị bỏ lại, cần dọn thủ công.",
                        publicId, request.ProductId, request.ImageId);
                }
            }
        }
    }
}
