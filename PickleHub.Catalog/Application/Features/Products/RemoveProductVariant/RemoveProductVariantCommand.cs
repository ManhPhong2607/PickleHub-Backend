using PickleHub.Common.Interfaces;
using MediatR;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.Exceptions;

namespace PickleHub.Catalog.Application.Features.Products.RemoveProductVariant
{
    public record RemoveProductVariantCommand(Guid ProductId, Guid VariantId) : IRequest;

    public class RemoveProductVariantHandler : IRequestHandler<RemoveProductVariantCommand>
    {
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RemoveProductVariantHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(RemoveProductVariantCommand request, CancellationToken ct)
        {
            var product = await _productRepository.GetByIdWithDetailAsync(request.ProductId, ct)
                ?? throw new NotFoundException("Sản phẩm không tồn tại.");

            product.RemoveVariant(request.VariantId);


            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
