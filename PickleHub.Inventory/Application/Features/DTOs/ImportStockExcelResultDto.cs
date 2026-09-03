namespace PickleHub.Inventory.Application.Features.DTOs
{
    public class ImportStockExcelResultDto
    {
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<ImportRowErrorDto> FailedRows { get; set; } = new();
    }
}
