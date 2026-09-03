using MassTransit;
using MediatR;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.Events.Catalog;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;

namespace PickleHub.Catalog.Application.Features.Products.DeleteProduct
{
    public record DeleteProductCommand(Guid Id) : IRequest;

    public class DeleteProductHandler : IRequestHandler<DeleteProductCommand>
    {
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ICurrentUserService _currentUser;

        public DeleteProductHandler(IProductRepository productRepository, IUnitOfWork unitOfWork, IPublishEndpoint publishEndpoint, ICurrentUserService currentUser)
        {
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
            _publishEndpoint = publishEndpoint;
            _currentUser = currentUser;
        }

        public async Task Handle(DeleteProductCommand request, CancellationToken ct)
        {
            var product = await _productRepository.GetByIdWithDetailAsync(request.Id, ct)
                ?? throw new NotFoundException("Sản phẩm không tồn tại.");

            _productRepository.Remove(product);
            await _unitOfWork.SaveChangesAsync(ct);

            await _publishEndpoint.Publish(new ProductStatusChangedEvent
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Action = "Deleted", 
                ActorUserId = _currentUser.UserId,
                ActorEmail = _currentUser.Email ?? string.Empty,
                OccurredAt = DateTime.UtcNow
            }, ct);
        }
    }
}
