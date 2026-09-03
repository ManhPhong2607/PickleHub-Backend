using MediatR;
using PickleHub.Catalog.Domain.Enums;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.Interfaces;

namespace PickleHub.Catalog.Application.Features.Products.BulkUpdateProductStatus
{
    public record BulkUpdateProductStatusCommand(List<Guid> ProductIds, ProductStatus Status) : IRequest<int>;

    public class BulkUpdateProductStatusHandler : IRequestHandler<BulkUpdateProductStatusCommand, int>
    {
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;

        public BulkUpdateProductStatusHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> Handle(BulkUpdateProductStatusCommand request, CancellationToken ct)
        {
            if (request.ProductIds == null || !request.ProductIds.Any())
                return 0;

            var products = await _productRepository.GetByIdsAsync(request.ProductIds, ct);

            foreach (var product in products)
            {
                product.SetStatus(request.Status);
                _productRepository.Update(product);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return products.Count;
        }
    }
}
