using MassTransit;
using MediatR;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.Events.Catalog;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;

namespace PickleHub.Catalog.Application.Features.Products.PublishProduct
{
    public record PublishProductCommand(Guid Id) : IRequest;

    public class PublishProductHandler : IRequestHandler<PublishProductCommand>
    {
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ICurrentUserService _currentUser;

        public PublishProductHandler(IProductRepository productRepository, IUnitOfWork unitOfWork, IPublishEndpoint publishEndpoint, ICurrentUserService currentUser)
        {
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
            _publishEndpoint = publishEndpoint;
            _currentUser = currentUser;
        }

        public async Task Handle(PublishProductCommand request, CancellationToken ct)
        {
            var product = await _productRepository.GetByIdWithDetailAsync(request.Id, ct)
                ?? throw new NotFoundException("Sản phẩm không tồn tại.");

            product.Publish();

            await _unitOfWork.SaveChangesAsync(ct);

            await _publishEndpoint.Publish(new ProductStatusChangedEvent
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Action = "Published", 
                ActorUserId = _currentUser.UserId,
                ActorEmail = _currentUser.Email ?? string.Empty,
                OccurredAt = DateTime.UtcNow
            }, ct);
        }
    }
}
