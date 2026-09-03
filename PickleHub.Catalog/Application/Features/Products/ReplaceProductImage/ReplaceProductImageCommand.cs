using CloudinaryDotNet.Actions;
using MediatR;
using PickleHub.Catalog.Application.Features.Products.DTOs;
using PickleHub.Catalog.Domain.Entities;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;

namespace PickleHub.Catalog.Application.Features.Products.ReplaceProductImage
{
    public record ReplaceProductImageCommand(Guid ProductId, Guid ImageId,IFormFile File) : IRequest<ProductImageDto>;

    public class ReplaceProductImageHandler : IRequestHandler<ReplaceProductImageCommand, ProductImageDto>
    {
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStorageService _storage;

        public ReplaceProductImageHandler(
            IProductRepository productRepository,
            IUnitOfWork unitOfWork,
            IStorageService storage)
        {
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
            _storage = storage;
        }
        public async Task<ProductImageDto> Handle(ReplaceProductImageCommand request, CancellationToken ct)
        {
            var product = await _productRepository.GetByIdWithDetailAsync(request.ProductId, ct)
                ?? throw new NotFoundException("Sản phẩm không tồn tại.");

            // Kiểm tra ảnh cần thay có tồn tại trước, tránh upload lên storage rồi mới biết ImageId sai.
            if (!product.Images.Any(i => i.Id == request.ImageId))
                throw new NotFoundException("Ảnh không tồn tại trong sản phẩm này.");

            await using var stream = request.File.OpenReadStream();
            string extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
            string resourceType = extension switch
            {
                ".mp4" or ".webm" or ".mov" => "video",
                _ => "image"
            };
            var uploadResult = await _storage.UploadAsync(
                fileStream: stream,
                fileName: request.File.FileName,
                folder: "products",
                resourceType: resourceType,
                ct: ct);

            ProductImage newImage;
            string oldPublicId;
            try
            {
                (newImage, oldPublicId) = product.ReplaceImage(
                    request.ImageId, uploadResult.PublicId, uploadResult.SecureUrl);
                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch
            {
                // Chỉ rollback (xóa ảnh mới) khi DOMAIN hoặc DB SAVE thất bại - lúc này
                // ảnh mới upload chưa được ai tham chiếu tới, xóa an toàn.
                await _storage.DeleteAsync(uploadResult.PublicId);
                throw;
            }
            // xóa asset cũ sau khi lưu ảnh mới thành công
            try
            {
                await _storage.DeleteAsync(oldPublicId);
            }
            catch
            {
                // Xóa ảnh cũ thất bại chỉ để lại rác trên Cloudinary (không ảnh hưởng tính đúng đắn của DB/response) - nuốt lỗi, không throw, không rollback.
            }

            return new ProductImageDto
            {
                Id = newImage.Id,
                PublicId = newImage.PublicId,
                Url = newImage.Url,
                VariantId = newImage.VariantId,
                SortOrder = newImage.SortOrder,
                IsSizeChart = newImage.IsSizeChart
            };
        }
    }
}
