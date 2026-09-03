using PickleHub.Common.Domain;
using PickleHub.Common.Exceptions;
using PickleHub.Inventory.Domain.Enums;

namespace PickleHub.Inventory.Domain.Entities
{
    public class InventoryItem : BaseEntity
    {
        public Guid ProductVariantId { get; private set; }
        public Guid ProductId { get; private set; }
        public string SkuSnapshot { get; private set; } = string.Empty;
        public int Quantity { get; private set; } = 0;               // Physical Stock
        public int ReservedQuantity { get; private set; } = 0;       // Reserved for pending orders
        public int LowStockThreshold { get; private set; } = 5;
        public uint Version { get; private set; }

        // Tồn kho vật lý (Quantity) = số lượng THẬT có trong kho, chỉ đổi khi hàng thật sựnhập vào/xuất ra khỏi kho (Import, Deduct-khi-Confirmed, Return).
        // Tồn kho khả dụng (AvailableQuantity) = phần CÓ THỂ bán tiếp ngay bây giờ, đã trừ đi phần đang bị giữ chỗ cho các đơn khác chưa xác nhận xong (ReservedQuantity).
        public int AvailableQuantity => Quantity - ReservedQuantity;
        public bool IsLowStock => AvailableQuantity > 0 && AvailableQuantity <= LowStockThreshold;
        public bool IsOutOfStock => AvailableQuantity <= 0;

        private readonly List<StockTransaction> _transactions = new();
        public IReadOnlyCollection<StockTransaction> Transactions => _transactions.AsReadOnly();

        private InventoryItem() { }

        public static InventoryItem Create(
            Guid productVariantId,
            Guid productId,
            string skuSnapshot,
            int lowStockThreshold = 5,
            int initialQuantity = 0
        )
        {
            var item = new InventoryItem
            {
                ProductVariantId = productVariantId,
                ProductId = productId,
                SkuSnapshot = skuSnapshot.Trim(),
                LowStockThreshold = lowStockThreshold
            };

            if (initialQuantity > 0)
            {
                item.Import(initialQuantity, note: "Khởi tạo tồn kho khả dụng");
            }

            return item;
        }
        public StockTransaction Import(int quantity, Guid? referenceId = null, string? note = null)
        {
            if (quantity <= 0)
                throw new DomainException("Số lượng nhập kho phải lớn hơn 0.");

            int pBefore = Quantity;
            int rBefore = ReservedQuantity;

            Quantity += quantity;
            Version++;
            SetUpdated();

            var transaction = StockTransaction.Create(
                Id, TransactionType.Import, quantity, referenceId, note);
            _transactions.Add(transaction);
            return transaction;
        }

        // Giữ chỗ tạm thời lúc checkout - CHƯA đụng tới Quantity (hàng vẫn nằm nguyên trong kho),
        // chỉ đánh dấu "đã có người giữ" để người khác không mua trùng lên phần này.
        public StockTransaction Reserve(int quantity, Guid orderId)
        {
            if (quantity <= 0)
                throw new DomainException("Số lượng giữ chỗ phải lớn hơn 0.");

            if (AvailableQuantity < quantity)
                throw new DomainException(
                    $"Tồn kho khả dụng không đủ. Khả dụng: {AvailableQuantity}, yêu cầu: {quantity}.");

            ReservedQuantity += quantity;
            Version++;
            SetUpdated();

            var transaction = StockTransaction.Create(Id, TransactionType.Reserve, quantity, orderId);
            _transactions.Add(transaction);
            return transaction;
        }

        // Nhả chỗ đã giữ (khách hủy checkout, đơn bị hủy khi còn Pending, hoặc thanh toán thất bại) - hàng chưa từng rời kho nên chỉ cần giảm ReservedQuantity, không đụng Quantity.
        public StockTransaction ReleaseReservation(int quantity, Guid orderId)
        {
            if (quantity <= 0)
                throw new DomainException("Số lượng nhả chỗ phải lớn hơn 0.");

            // Clamp về 0 thay vì throw nếu lỡ nhả nhiều hơn đang giữ (dữ liệu lệch do race condition hiếm gặp) - ưu tiên không chặn luồng hủy đơn của khách vì lỗi này.
            ReservedQuantity = Math.Max(0, ReservedQuantity - quantity);
            Version++;
            SetUpdated();

            var transaction = StockTransaction.Create(Id, TransactionType.ReleaseReservation, quantity, orderId);
            _transactions.Add(transaction);
            return transaction;
        }

        // Chốt phần đã giữ chỗ thành xuất kho thật (đơn được Confirmed/thanh toán xong) -
        // hàng chính thức rời kho: Quantity VÀ ReservedQuantity cùng giảm.
        public StockTransaction Deduct(int quantity, Guid orderId)
        {
            if (quantity <= 0)
                throw new DomainException("Số lượng trừ kho phải lớn hơn 0.");

            if (Quantity < quantity)
                throw new DomainException(
                    $"Tồn kho vật lý không đủ. Hiện có: {Quantity}, yêu cầu: {quantity}.");

            Quantity -= quantity;
            // Clamp về 0 - phòng trường hợp đơn hàng cũ (tạo trước khi có cơ chế Reserve)
            // không hề có ReservedQuantity tương ứng, tránh làm số bị âm.
            ReservedQuantity = Math.Max(0, ReservedQuantity - quantity);
            Version++;
            SetUpdated();

            var transaction = StockTransaction.Create(Id, TransactionType.Deduct, quantity, orderId);
            _transactions.Add(transaction);

            return transaction;
        }

        public StockTransaction Return(int quantity, Guid orderId)
        {
            if (quantity <= 0)
                throw new DomainException("Số lượng hoàn kho phải lớn hơn 0.");

            Quantity += quantity;
            Version++;
            SetUpdated();

            var transaction = StockTransaction.Create(Id, TransactionType.Return, quantity, orderId);
            _transactions.Add(transaction);
            return transaction;
        }

        public void UpdateThreshold(int threshold)
        {
            if (threshold < 0)
                throw new DomainException("Ngưỡng cảnh báo không được âm.");

            LowStockThreshold = threshold;
            Version++;
            SetUpdated();
        }
    }
}
