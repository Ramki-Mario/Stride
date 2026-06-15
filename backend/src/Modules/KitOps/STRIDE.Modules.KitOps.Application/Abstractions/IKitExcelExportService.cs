using STRIDE.Modules.KitOps.Application.DTOs;

namespace STRIDE.Modules.KitOps.Application.Abstractions;

public interface IKitExcelExportService
{
    byte[] ExportCheckoutHistory(IReadOnlyList<KitCheckoutReportRowDto> rows);
    byte[] ExportUsageSummary(IReadOnlyList<KitUsageSummaryRowDto> rows);
}
