using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.CartOrder.Domain.Entities;
using PickleHub.Common.Enums;
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
    string? PaymentUrl = null
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

        // Gọi Customer Service lấy địa chỉ từ Sổ địa chỉ
        var address = await customerClient.GetAddressByIdAsync(request.AddressId, ct);
        if (address is null)
        {
            throw new KeyNotFoundException($"Không tìm thấy thông tin địa chỉ giao hàng với mã ID {request.AddressId}.");
        }

        var shippingFullName = address.FullName;
        var shippingPhone = address.PhoneNumber;
        var shippingProvince = address.Province;
        var shippingDistrict = address.District;
        var shippingWard = address.Ward;
        var shippingStreetAddress = address.StreetAddress;

        // Gọi Customer Service lấy thông tin Email khách hàng
        var customer = await customerClient.GetCustomerDetailsAsync(request.UserId, ct);
        if (customer is null)
        {
            throw new KeyNotFoundException($"Không tìm thấy thông tin tài khoản khách hàng với mã ID {request.UserId}.");
        }

        var customerName = customer.FullName;
        var customerEmail = customer.Email;

        // Khởi tạo danh sách OrderItem và kiểm tra tồn kho & giá
        var orderId = Guid.NewGuid();
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
                    throw new KeyNotFoundException($"Sản phẩm hoặc biến thể ID {targetVariantId} không tồn tại trong hệ thống Catalog.");
                }

                // Nếu từ đầu phát hiện không đủ tồn kho, ta sẽ không giữ chỗ nữa mà đánh dấu là thiếu hàng
                if (isAllStockAvailable)
                {
                    var reserveSuccess = await inventoryClient.ReserveStockAsync(targetVariantId, cartItem.Quantity, ct);
                    if (!reserveSuccess)
                    {
                        isAllStockAvailable = false;
                        // Giải phóng toàn bộ tồn kho đã giữ chỗ trước đó do không đủ hàng đồng bộ
                        foreach (var reserved in reservedItems)
                        {
                            await inventoryClient.ReleaseStockAsync(reserved.ProductVariantId, reserved.Quantity, ct);
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
                await inventoryClient.ReleaseStockAsync(reserved.ProductVariantId, reserved.Quantity, ct);
            }
            throw;
        }

        decimal shippingFee = await systemClient.GetDefaultShippingFeeAsync(ct);

        // Loyalty áp dụng trên Subtotal (đã bao gồm sale từ Catalog) - KHÔNG áp lên phí ship.
        // customer.LoyaltyDiscountPercent lấy từ Customer Service, tính động theo TotalSpent
        // hiện tại - CartOrder không tự lưu/tính % này, chỉ đọc kết quả.
        var loyaltyDiscountPercent = customer.LoyaltyDiscountPercent;
        var loyaltyDiscountAmount = loyaltyDiscountPercent > 0
            ? Math.Round(subtotal * (loyaltyDiscountPercent / 100m), 0)
            : 0m;

        decimal totalAmount = (subtotal - loyaltyDiscountAmount) + shippingFee;

        // Logic tự động xác nhận:
        // Đơn COD + Đủ hàng -> Tự động OrderStatus.Confirmed
        // Đơn PayOS HOẶC Thiếu hàng -> OrderStatus.Pending (Chờ thanh toán / Chờ Admin duyệt kho)
        var isCod = request.PaymentMethod.Equals("COD", StringComparison.OrdinalIgnoreCase);
        var initialStatus = (isCod && isAllStockAvailable) ? OrderStatus.Confirmed : OrderStatus.Pending;

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
        if (request.PaymentMethod.Equals("PayOS", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var paymentResult = await paymentClient.CreatePaymentLinkAsync(orderId, totalAmount, ct);
                paymentUrl = paymentResult?.CheckoutUrl;
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
                    await inventoryClient.ReleaseStockAsync(reserved.ProductVariantId, reserved.Quantity, ct);
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

        // Nếu là đơn COD đủ kho (tự động Confirmed) -> Publish thêm OrderStatusUpdatedEvent để gửi email xác nhận đã duyệt
        if (initialStatus == OrderStatus.Confirmed)
        {
            await publishEndpoint.Publish(new OrderStatusUpdatedEvent
            {
                OrderId = orderId,
                CustomerId = request.UserId,
                CustomerName = customerName,
                CustomerEmail = customerEmail,
                OldStatus = PickleHub.Common.Enums.OrderStatus.Pending,
                NewStatus = PickleHub.Common.Enums.OrderStatus.Confirmed,
                UpdatedAt = DateTime.UtcNow
            }, ct);
        }

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
            paymentUrl
        );
    }
}