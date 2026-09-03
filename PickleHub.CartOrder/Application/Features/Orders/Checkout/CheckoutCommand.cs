using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.CartOrder.Domain.Entities;
using PickleHub.Common.Enums;
using PickleHub.Common.Exceptions;
using PickleHub.CartOrder.Domain.Interfaces;
using PickleHub.CartOrder.Infrastructure.Persistence;
using PickleHub.Common.Events.Order;

namespace PickleHub.CartOrder.Application.Features.Orders.Checkout;

// Command đặt hàng (Checkout).
public record CheckoutCommand(
    Guid UserId,
    Guid AddressId,
    string PaymentMethod = "COD",
    string? Note = null
) : IRequest<CheckoutResponse>;

public record CheckoutResponse(
    Guid OrderId,
    decimal Subtotal,
    decimal LoyaltyDiscountPercent,
    decimal LoyaltyDiscountAmount,
    decimal ShippingFee,
    decimal TotalAmount,
    string Status,
    string PaymentMethod,
    string PaymentStatus,
    string? PaymentUrl = null,
    string? QrCode = null
);

public class CheckoutCommandHandler(
    CartOrderDbContext db,
    ICatalogClient catalogClient,
    IInventoryClient inventoryClient,
    ICustomerClient customerClient,
    ISystemClient systemClient,
    IPaymentClient paymentClient,
    IPublishEndpoint publishEndpoint
) : IRequestHandler<CheckoutCommand, CheckoutResponse>
{
    public async Task<CheckoutResponse> Handle(CheckoutCommand request, CancellationToken ct)
    {
        db.ChangeTracker.Clear();

        // Lấy giỏ hàng của người dùng kèm danh sách sản phẩm
        var cart = await db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, ct);

        if (cart is null || cart.Items.Count == 0)
        {
            throw new InvalidOperationException("Giỏ hàng của bạn đang trống, hãy thêm sản phẩm trước khi đặt hàng.");
        }

        // Khởi tạo OrderId trước để dùng làm referenceId đồng bộ cho Inventory và các Service
        var orderId = Guid.NewGuid();

        // Gọi Customer Service lấy địa chỉ từ Sổ địa chỉ
        var address = await customerClient.GetAddressByIdAsync(request.AddressId, ct);
        if (address is null)
        {
            throw new NotFoundException($"Không tìm thấy thông tin địa chỉ giao hàng với mã ID {request.AddressId}.");
        }

        var shippingFullName = address.FullName;
        var shippingPhone = address.PhoneNumber;
        var shippingProvince = address.Province;
        var shippingDistrict = address.District;
        var shippingWard = address.Ward;
        var shippingStreetAddress = address.StreetAddress;

        // Gọi Customer Service lấy thông tin Email khách hàng (fallback an toàn nếu profile chưa tạo)
        var customer = await customerClient.GetCustomerDetailsAsync(request.UserId, ct);
        var customerName = customer?.FullName ?? shippingFullName;
        var customerEmail = customer?.Email ?? string.Empty;
        var loyaltyDiscountPercent = customer?.LoyaltyDiscountPercent ?? 0m;

        // Khởi tạo danh sách OrderItem và kiểm tra tồn kho & giá
        var orderItems = new List<OrderItem>();
        var subtotal = 0m;
        var eventItems = new List<OrderItemPayload>();
        var isAllStockAvailable = true;
        var reservedItems = new List<(Guid ProductVariantId, int Quantity)>();

        try
        {
            // Lặp qua từng item trong giỏ để kiểm tra nghiệp vụ chéo qua các Service khác
            foreach (var cartItem in cart.Items)
            {
                var targetVariantId = cartItem.ProductVariantId != Guid.Empty ? cartItem.ProductVariantId : cartItem.ProductId;

                var product = await catalogClient.GetProductDetailsAsync(targetVariantId, ct);
                if (product is null)
                {
                    throw new NotFoundException($"Sản phẩm hoặc biến thể ID {targetVariantId} không tồn tại trong hệ thống Catalog.");
                }

                // Nếu từ đầu phát hiện không đủ tồn kho, ta sẽ không giữ chỗ nữa mà đánh dấu là thiếu hàng
                if (isAllStockAvailable)
                {
                    var reserveSuccess = await inventoryClient.ReserveStockAsync(targetVariantId, cartItem.Quantity, orderId, ct);
                    if (!reserveSuccess)
                    {
                        isAllStockAvailable = false;
                        // Giải phóng toàn bộ tồn kho đã giữ chỗ trước đó do không đủ hàng đồng bộ
                        foreach (var reserved in reservedItems)
                        {
                            await inventoryClient.ReleaseStockAsync(reserved.ProductVariantId, reserved.Quantity, orderId, ct);
                        }
                        reservedItems.Clear();
                    }
                    else
                    {
                        reservedItems.Add((targetVariantId, cartItem.Quantity));
                    }
                }

                var variants = product.Variants ?? new List<CatalogProductVariantDto>();
                var images = product.Images ?? new List<CatalogProductImageDto>();

                var variant = variants.FirstOrDefault(v => v.Id == targetVariantId);
                // Giá sau sale (nếu có) - Catalog đã tính sẵn EffectivePrice, CartOrder không tự tính lại
                // % sale ở đây, tránh 2 nơi cùng biết logic tính sale (chỉ Catalog sở hữu dữ liệu đó).
                var unitPrice = variant?.EffectivePrice ?? variant?.Price ?? product.BasePrice;
                var itemSubtotal = unitPrice * cartItem.Quantity;
                subtotal += itemSubtotal;

                var imageUrl = images
                    .Where(img => !img.IsSizeChart)
                    .OrderBy(img => img.SortOrder)
                    .FirstOrDefault()?.Url ?? cartItem.ImageUrlSnapshot;

                orderItems.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    ProductVariantId = targetVariantId,
                    ProductId = product.Id,
                    ProductNameSnapshot = product.Name,
                    VariantAttributesSnapshot = variant?.Sku ?? string.Empty,
                    ImageUrlSnapshot = imageUrl,
                    UnitPrice = unitPrice,
                    Quantity = cartItem.Quantity,
                    Subtotal = itemSubtotal,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                eventItems.Add(new OrderItemPayload
                {
                    ProductId = product.Id,
                    ProductVariantId = targetVariantId,
                    ProductNameSnapshot = product.Name,
                    VariantAttributesSnapshot = variant?.Sku ?? string.Empty,
                    Quantity = cartItem.Quantity,
                    UnitPrice = unitPrice
                });
            }
        }
        catch (Exception)
        {
            // Giải phóng toàn bộ tồn kho đã giữ chỗ nếu có lỗi bất kỳ trong quá trình xử lý loop
            foreach (var reserved in reservedItems)
            {
                await inventoryClient.ReleaseStockAsync(reserved.ProductVariantId, reserved.Quantity, orderId, ct);
            }
            throw;
        }

        decimal shippingFee = await systemClient.GetDefaultShippingFeeAsync(ct);

        // Loyalty áp dụng trên Subtotal (đã bao gồm sale từ Catalog) - KHÔNG áp lên phí ship.
        var loyaltyDiscountAmount = loyaltyDiscountPercent > 0
            ? Math.Round(subtotal * (loyaltyDiscountPercent / 100m), 0)
            : 0m;

        decimal totalAmount = (subtotal - loyaltyDiscountAmount) + shippingFee;

        // Logic xác nhận đơn: Tất cả đơn hàng mới tạo (kể cả COD hay PayOS)
        // đều luôn ở trạng thái Pending (Chờ xác nhận) cho đến khi Admin chủ động xác nhận duyệt đơn.
        var initialStatus = OrderStatus.Pending;

        var order = new Order
        {
            Id = orderId,
            CustomerId = request.UserId,
            Status = initialStatus,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = PaymentStatus.Unpaid,
            IsStockReserved = isAllStockAvailable,
            ShippingFullName = shippingFullName,
            ShippingPhone = shippingPhone,
            ShippingProvince = shippingProvince,
            ShippingDistrict = shippingDistrict,
            ShippingWard = shippingWard,
            ShippingStreetAddress = shippingStreetAddress,
            Subtotal = subtotal,
            LoyaltyDiscountPercent = loyaltyDiscountPercent,
            LoyaltyDiscountAmount = loyaltyDiscountAmount,
            ShippingFee = shippingFee,
            TotalAmount = totalAmount,
            Note = request.Note,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = orderItems
        };

        // 4. Lưu đơn hàng vào DB trước để Payment Service có thể thực hiện đối soát số tiền (verify) thành công qua HTTP
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        // 5. Nếu là đơn PayOS, gọi Payment Service để sinh liên kết thanh toán QR Code
        string? paymentUrl = null;
        string? qrCode = null;
        if (request.PaymentMethod.Equals("PayOS", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var paymentResult = await paymentClient.CreatePaymentLinkAsync(orderId, totalAmount, ct);
                paymentUrl = paymentResult?.CheckoutUrl;
                qrCode = paymentResult?.QrCode;
                if (string.IsNullOrEmpty(paymentUrl))
                {
                    throw new Exception("Không nhận được URL thanh toán từ cổng PayOS.");
                }
            }
            catch (Exception ex)
            {
                // Compensating Action: Xoá đơn hàng vừa lưu khỏi DB để bảo toàn trạng thái
                db.Orders.Remove(order);
                await db.SaveChangesAsync(ct);

                // Giải phóng toàn bộ tồn kho đã giữ chỗ trước đó do lỗi cổng thanh toán
                foreach (var reserved in reservedItems)
                {
                    await inventoryClient.ReleaseStockAsync(reserved.ProductVariantId, reserved.Quantity, orderId, ct);
                }

                throw new Exception($"Không thể hoàn tất Checkout do lỗi cổng thanh toán: {ex.Message}", ex);
            }
        }

        // 6. Checkout thành công -> Xoá giỏ hàng
        db.CartItems.RemoveRange(cart.Items);
        await db.SaveChangesAsync(ct);

        // Publish OrderCreatedEvent để Inventory trừ kho & Notification gửi email
        await publishEndpoint.Publish(new OrderCreatedEvent
        {
            OrderId = orderId,
            CustomerId = request.UserId,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            ShippingFullName = shippingFullName,
            ShippingPhone = shippingPhone,
            ShippingAddress = $"{shippingStreetAddress}, {shippingWard}, {shippingDistrict}, {shippingProvince}",
            Items = eventItems,
            TotalAmount = totalAmount,
            PaymentMethod = request.PaymentMethod,
            CreatedAt = order.CreatedAt
        }, ct);

        return new CheckoutResponse(
            order.Id,
            order.Subtotal,
            order.LoyaltyDiscountPercent,
            order.LoyaltyDiscountAmount,
            order.ShippingFee,
            order.TotalAmount,
            order.Status.ToString(),
            order.PaymentMethod,
            order.PaymentStatus.ToString(),
            paymentUrl,
            qrCode
        );
    }
}