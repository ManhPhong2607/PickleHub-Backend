using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PickleHub.Catalog.Infrastructure.Persistence;
using PickleHub.Common.Events.Order;

namespace PickleHub.Catalog.Infrastructure.Consumers;

/// <summary>
/// Lắng nghe sự kiện OrderCreatedEvent để tự động tăng SoldCount cho sản phẩm trong Catalog DB.
/// </summary>
public class OrderCreatedConsumer(
    CatalogDbContext db,
    ILogger<OrderCreatedConsumer> logger
) : IConsumer<OrderCreatedEvent>
{
    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;
        if (message.Items == null || message.Items.Count == 0)
        {
            return;
        }

        logger.LogInformation("[Catalog.OrderCreatedConsumer] Nhận OrderCreatedEvent cho OrderId [{OrderId}] với {Count} món hàng", message.OrderId, message.Items.Count);

        var updatedCount = 0;

        foreach (var item in message.Items)
        {
            try
            {
                var productId = item.ProductId;

                // Nếu chưa có ProductId từ payload, tra cứu thông qua ProductVariantId
                if (productId == Guid.Empty && item.ProductVariantId != Guid.Empty)
                {
                    var variant = await db.ProductVariants.FirstOrDefaultAsync(v => v.Id == item.ProductVariantId, context.CancellationToken);
                    if (variant != null)
                    {
                        productId = variant.ProductId;
                    }
                }

                if (productId != Guid.Empty)
                {
                    var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId, context.CancellationToken);
                    if (product != null)
                    {
                        product.IncreaseSoldCount(item.Quantity > 0 ? item.Quantity : 1);
                        updatedCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Catalog.OrderCreatedConsumer] Lỗi cập nhật SoldCount cho VariantId [{VariantId}]", item.ProductVariantId);
            }
        }

        if (updatedCount > 0)
        {
            await db.SaveChangesAsync(context.CancellationToken);
            logger.LogInformation("[Catalog.OrderCreatedConsumer] Đã tăng SoldCount thành công cho {UpdatedCount} sản phẩm từ Order [{OrderId}]", updatedCount, message.OrderId);
        }
    }
}
