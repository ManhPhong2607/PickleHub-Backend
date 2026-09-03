using MediatR;
using PickleHub.Catalog.Application.Features.Products.DTOs;
using PickleHub.Catalog.Domain.Entities;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;

namespace PickleHub.Catalog.Application.Features.Products.AddProductImage
{
    public record AddProductImageCommand(
       Guid ProductId,
       List<IFormFile> Files,
       Guid? VariantId = null,
       bool IsSizeChart = false
    ) : IRequest<List<ProductImageDto>>;

    public class AddProductImageHandler : IRequestHandler<AddProductImageCommand, List<ProductImageDto>>
    {
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStorageService _storage;

        public AddProductImageHandler(
            IProductRepository productRepository,
            IUnitOfWork unitOfWork,
            IStorageService storage)
        {
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
            _storage = storage;
        }

        public async Task<List<ProductImageDto>> Handle(AddProductImageCommand request, CancellationToken ct)
        {
            var product = await _productRepository.GetByIdWithDetailAsync(request.ProductId, ct)
                ?? throw new NotFoundException("Sản phẩm không tồn tại.");

            // Precheck TRƯỚC khi tốn công upload lên storage
            product.EnsureCanAddImages(request.VariantId, request.IsSizeChart, request.Files.Count);

            // Upload tất cả các file lên Storage song song cùng lúc (Fast Parallel Upload).
            // Task.WhenAll giữ nguyên chỉ số thứ tự của mảng kết quả trùng với thứ tự request.Files ban đầu.
            var uploadTasks = request.Files.Select(async file =>
            {
                await using var stream = file.OpenReadStream();
                string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                string resourceType = extension switch
                {
                    ".mp4" or ".webm" or ".mov" => "video",
                    _ => "image"
                };

                return await _storage.UploadAsync(
                    fileStream: stream,
                    fileName: file.FileName,
                    folder: "products",
                    resourceType: resourceType,
                    ct: ct);
            });

            var uploadResults = await Task.WhenAll(uploadTasks);

            var addedImages = new List<ProductImage>();
            try
            {
                foreach (var uploadResult in uploadResults)
                {
                    var image = product.AddImage(
                        uploadResult.PublicId,
                        uploadResult.SecureUrl,
                        request.VariantId,
                        request.IsSizeChart);

                    addedImages.Add(image);
                }

                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch
            {
                // Nếu bị lỗi lúc lưu DB, tiến hành dọn dẹp (cleanup) toàn bộ file vừa upload
                foreach (var uploadResult in uploadResults)
                {
                    await _storage.DeleteAsync(uploadResult.PublicId);
                }
                throw;
            }

            return addedImages.Select(image => new ProductImageDto
            {
                Id = image.Id,
                PublicId = image.PublicId,
                Url = image.Url,
                VariantId = image.VariantId,
                SortOrder = image.SortOrder,
                IsSizeChart = image.IsSizeChart
            }).ToList();
        }
    }
}
