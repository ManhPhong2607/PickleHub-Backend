namespace PickleHub.Inventory.Domain.Enums
{
    public enum TransactionType
    {
        Import, // nhập kho
        Reserve, // giữ chỗ
        ReleaseReservation, // nhả giữ chỗ
        Deduct, // trừ kho thật
        Return  // hoàn kho
    }
}
