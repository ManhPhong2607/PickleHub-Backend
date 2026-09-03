using ClosedXML.Excel;
using MediatR;
using PickleHub.Inventory.Application.Common.Interfaces;
using PickleHub.Inventory.Domain.Repositories;

namespace PickleHub.Inventory.Application.Features.Inventory.ExportInventoryExcel
{
    // Khác với ExportInventoryToExcelQuery (báo cáo, chỉ để xem): file này dùng để IMPORT lại,
    // nên cột số lượng luôn để TRỐNG (không phải số tồn hiện tại) - tránh admin nhầm số tồn kho
    // với số muốn nhập thêm, gây nhân đôi tồn kho khi import ngược lại.
    public record GetImportTemplateExcelQuery : IRequest<byte[]>;

    public class GetImportTemplateExcelHandler : IRequestHandler<GetImportTemplateExcelQuery, byte[]>
    {
        private readonly IInventoryItemRepository _inventoryRepository;
        private readonly ICatalogClient _catalogClient;

        public GetImportTemplateExcelHandler(
            IInventoryItemRepository inventoryRepository,
            ICatalogClient catalogClient)
        {
            _inventoryRepository = inventoryRepository;
            _catalogClient = catalogClient;
        }

        public async Task<byte[]> Handle(GetImportTemplateExcelQuery request, CancellationToken ct)
        {
            var items = await _inventoryRepository.GetAllAsync(ct);
            var existingVariantIds = items.Select(i => i.ProductVariantId).ToHashSet();

            // Hỏi Catalog toàn bộ variant đang bán -> đối chiếu với những gì Inventory đã có,
            // variant nào chưa từng xuất hiện trong InventoryItem thì đây là "chưa nhập kho lần nào".
            // Nếu Catalog không gọi được, danh sách trả về rỗng (fail-soft) - template vẫn xuất ra
            // bình thường, chỉ là thiếu phần "gợi ý sản phẩm mới" thôi, không chặn cả tính năng.
            var allVariants = await _catalogClient.GetAllVariantsAsync(ct);
            var neverStocked = allVariants
                .Where(v => !existingVariantIds.Contains(v.VariantId))
                .ToList();

            // Sắp hết/hết hàng lên đầu - đây là nhóm admin cần nhập kho gấp nhất,
            // nên xuất hiện ngay khi mở file thay vì phải cuộn tìm.
            var ordered = items
                .OrderBy(i => i.IsOutOfStock ? 0 : i.IsLowStock ? 1 : 2)
                .ThenBy(i => i.Quantity)
                .ToList();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Template nhập kho");

            var headers = new[]
            {
                "ProductVariantId", "ProductId", "SKU", "Tồn kho hiện tại (chỉ để tham khảo)",
                "Số lượng nhập thêm", "Ngưỡng cảnh báo mới (để trống = giữ nguyên)", "Ghi chú"
            };
            for (int col = 0; col < headers.Length; col++)
                sheet.Cell(1, col + 1).Value = headers[col];

            var headerRow = sheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#f8f9fa");

            var row = 2;

            // 1. Duyệt qua toàn bộ sản phẩm hiện có theo đúng thứ tự như file Xuất Excel
            foreach (var item in items)
            {
                sheet.Cell(row, 1).Value = item.ProductVariantId.ToString();
                sheet.Cell(row, 2).Value = item.ProductId.ToString();
                sheet.Cell(row, 3).Value = item.SkuSnapshot;
                sheet.Cell(row, 4).Value = item.Quantity; // chỉ tham khảo, KHÔNG dùng cột này khi import
                // Cột 5 (Số lượng nhập thêm) cố tình để trống - admin tự điền.
                sheet.Cell(row, 5).Value = string.Empty;
                // Cột 6 (Ngưỡng cảnh báo mới) để trống = giữ nguyên.
                sheet.Cell(row, 6).Value = string.Empty;
                // Cột 7 (Ghi chú) để trống.
                sheet.Cell(row, 7).Value = string.Empty;

                // Tô màu đỏ nhạt cho các sản phẩm sắp hết hàng hoặc đã hết hàng
                if (item.IsOutOfStock || item.IsLowStock || item.Quantity <= item.LowStockThreshold)
                {
                    sheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#fee2e2");
                }

                row++;
            }

            // 2. Các sản phẩm mới trên Catalog nhưng CHƯA TỪNG nhập kho (nếu có) được thêm ở cuối
            foreach (var v in neverStocked)
            {
                sheet.Cell(row, 1).Value = v.VariantId.ToString();
                sheet.Cell(row, 2).Value = v.ProductId.ToString();
                sheet.Cell(row, 3).Value = v.Sku;
                sheet.Cell(row, 4).Value = "Chưa nhập kho";
                sheet.Cell(row, 5).Value = string.Empty;
                sheet.Cell(row, 6).Value = string.Empty;
                sheet.Cell(row, 7).Value = string.Empty;
                sheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#eff6ff");
                row++;
            }

            // Cột "Tồn kho hiện tại" tô xám để nhấn mạnh đây là dữ liệu chỉ-để-xem, đừng sửa,
            // và tuyệt đối không nhầm với cột "Số lượng nhập thêm" bên cạnh.
            sheet.Column(4).Style.Font.FontColor = XLColor.Gray;
            sheet.Column(4).Style.Font.Italic = true;

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
