using ClosedXML.Excel;
using MediatR;
using PickleHub.Inventory.Domain.Repositories;

namespace PickleHub.Inventory.Application.Features.Inventory.ExportInventoryExcel
{
    public record ExportInventoryToExcelQuery : IRequest<byte[]>;

    public class ExportInventoryToExcelHandler : IRequestHandler<ExportInventoryToExcelQuery, byte[]>
    {
        private readonly IInventoryItemRepository _inventoryRepository;

        public ExportInventoryToExcelHandler(IInventoryItemRepository inventoryRepository)
        {
            _inventoryRepository = inventoryRepository;
        }

        public async Task<byte[]> Handle(ExportInventoryToExcelQuery request, CancellationToken ct)
        {
            var items = await _inventoryRepository.GetAllAsync(ct);

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Tồn kho");

            // Cố ý giữ đúng thứ tự cột này để admin có thể sửa Quantity/Threshold rồi
            // import ngược lại file này - không cần tra cứu VariantId/ProductId ở đâu khác.
            var headers = new[]
            {
                "ProductVariantId", "ProductId", "SKU", "Số lượng hiện tại",
                "Ngưỡng cảnh báo", "Trạng thái"
            };
            for (int col = 0; col < headers.Length; col++)
                sheet.Cell(1, col + 1).Value = headers[col];

            var headerRow = sheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#f8f9fa");

            var row = 2;
            foreach (var item in items)
            {
                var status = item.IsOutOfStock ? "Hết hàng" : item.IsLowStock ? "Sắp hết" : "Bình thường";

                sheet.Cell(row, 1).Value = item.ProductVariantId.ToString();
                sheet.Cell(row, 2).Value = item.ProductId.ToString();
                sheet.Cell(row, 3).Value = item.SkuSnapshot;
                sheet.Cell(row, 4).Value = item.Quantity;
                sheet.Cell(row, 5).Value = item.ReservedQuantity;
                sheet.Cell(row, 6).Value = item.AvailableQuantity;
                sheet.Cell(row, 7).Value = item.LowStockThreshold;
                sheet.Cell(row, 8).Value = status;
                sheet.Cell(row, 9).Value = (item.UpdatedAt ?? item.CreatedAt).ToString("yyyy-MM-dd HH:mm");
                row++;
            }

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }

}
