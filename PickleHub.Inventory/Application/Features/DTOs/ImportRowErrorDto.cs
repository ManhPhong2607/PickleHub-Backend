namespace PickleHub.Inventory.Application.Features.DTOs
{
    public class ImportRowErrorDto
    {
        public int RowNumber { get; set; }
        public string? Sku { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}