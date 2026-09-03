using PickleHub.Common.Interfaces;
using MediatR;
using PickleHub.Common.Exceptions;
using PickleHub.Inventory.Domain.Repositories;

namespace PickleHub.Inventory.Application.Features.Inventory.DeleteInventoryItem
{
    public record DeleteInventoryItemCommand(Guid VariantId) : IRequest;

    public class DeleteInventoryItemHandler : IRequestHandler<DeleteInventoryItemCommand>
    {
        private readonly IInventoryItemRepository _inventoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteInventoryItemHandler(
            IInventoryItemRepository inventoryRepository,
            IUnitOfWork unitOfWork)
        {
            _inventoryRepository = inventoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteInventoryItemCommand request, CancellationToken ct)
        {
            var item = await _inventoryRepository
                .GetByVariantIdAsync(request.VariantId, ct)
                ?? throw new NotFoundException("Không tìm thấy thông tin tồn kho.");

            _inventoryRepository.Remove(item);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
