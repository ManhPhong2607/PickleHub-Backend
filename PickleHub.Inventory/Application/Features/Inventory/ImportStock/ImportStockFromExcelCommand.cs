using ClosedXML.Excel;
using MediatR;
using PickleHub.Inventory.Application.Features.DTOs;

namespace PickleHub.Inventory.Application.Features.Inventory.ImportStock
{
    public record ImportStockFromExcelCommand(Stream FileStream) : IRequest<ImportStockExcelResultDto>;

    // Cột theo đúng thứ tự file GetImportTemplateExcelQuery xuất ra:
    // A=ProductVariantId, B=ProductId, C=SKU, D=Tồn kho hiện tại (CHỈ THAM KHẢO - không đọc cột này khi import),
    // E=Số lượng nhập thêm, F=Ngưỡng cảnh báo mới (bỏ trống = giữ nguyên), G=Ghi chú
    // Lưu ý: cột E là số lượng NHẬP THÊM (cộng dồn vào tồn kho hiện có), không phải số lượng tồn kho cuối cùng - khớp đúng nghĩa "Import" đã có ở ImportStockCommand.
    public class ImportStockFromExcelHandler : IRequestHandler<ImportStockFromExcelCommand, ImportStockExcelResultDto>
    {
        private readonly ISender _mediator;

        public ImportStockFromExcelHandler(ISender mediator)
        {
            _mediator = mediator;
        }

        public async Task<ImportStockExcelResultDto> Handle(ImportStockFromExcelCommand request, CancellationToken ct)
        {
            var result = new ImportStockExcelResultDto();

            using var workbook = new XLWorkbook(request.FileStream);
            var sheet = workbook.Worksheets.First();

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

            for (int rowNumber = 2; rowNumber <= lastRow; rowNumber++) // dòng 1 là header
            {
                var row = sheet.Row(rowNumber);
                if (row.IsEmpty()) continue;

                var skuCell = row.Cell(3).GetString().Trim();

                try
                {
                    var variantIdRaw = row.Cell(1).GetString().Trim();
                    var productIdRaw = row.Cell(2).GetString().Trim();
                    // Cột 4 (Tồn kho hiện tại) CỐ TÌNH bỏ qua - chỉ để admin tham khảo, không phải input.
                    var quantityRaw = row.Cell(5).GetString().Trim();
                    var thresholdRaw = row.Cell(6).GetString().Trim();
                    var note = row.Cell(7).GetString().Trim();

                    if (!Guid.TryParse(variantIdRaw, out var variantId))
                        throw new FormatException($"ProductVariantId không hợp lệ: '{variantIdRaw}'");

                    if (!Guid.TryParse(productIdRaw, out var productId))
                        throw new FormatException($"ProductId không hợp lệ: '{productIdRaw}'");

                    // Dòng chưa điền số lượng nhập (admin chỉ tải template về xem, chưa quyết định nhập)
                    // -> bỏ qua êm, không tính là lỗi, không đưa vào FailedRows.
                    if (string.IsNullOrWhiteSpace(quantityRaw)) continue;

                    if (!int.TryParse(quantityRaw, out var quantity) || quantity <= 0)
                        throw new FormatException($"Số lượng nhập không hợp lệ: '{quantityRaw}' (phải là số nguyên dương)");

                    if (string.IsNullOrWhiteSpace(skuCell))
                        throw new FormatException("Thiếu SKU");

                    // Tái dùng nguyên logic ImportStockCommand đã có (retry concurrency, publish  StockImportedEvent, tự tạo InventoryItem nếu đây là variant chưa từng nhập kho)
                    // - không viết lại logic nhập kho lần 2 ở đây.
                    await _mediator.Send(new ImportStockCommand(
                        variantId, productId, skuCell,
                        quantity,
                        string.IsNullOrWhiteSpace(note) ? $"Import từ Excel - dòng {rowNumber}" : note), ct);

                    // Ngưỡng cảnh báo là optional - chỉ update nếu admin có điền ở cột này.
                    if (int.TryParse(thresholdRaw, out var threshold) && threshold >= 0)
                    {
                        await _mediator.Send(new UpdateThreshold.UpdateThresholdCommand(variantId, threshold), ct);
                    }

                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    // Lỗi 1 dòng KHÔNG được làm dừng cả file - ghi nhận lại rồi xử lý tiếp dòng sau,
                    // admin cần biết chính xác dòng nào lỗi để sửa và import lại riêng dòng đó.
                    result.FailedCount++;
                    result.FailedRows.Add(new ImportRowErrorDto
                    {
                        RowNumber = rowNumber,
                        Sku = string.IsNullOrWhiteSpace(skuCell) ? null : skuCell,
                        Reason = ex.Message
                    });
                }
            }

            return result;
        }
    }
}


