namespace PickleHub.Catalog.Domain.Enums
{
    public enum PromotionStatus
    {
        Active = 1,      // Đang diễn ra trong thời hạn và IsActive = true
        Scheduled = 2,   // Chưa tới ngày bắt đầu và IsActive = true
        Expired = 3,     // Đã quá ngày kết thúc
        Disabled = 4     // Bị admin tắt chủ động (IsActive = false)
    }
}
