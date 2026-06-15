using ClosedXML.Excel;
using STRIDE.Modules.KitOps.Application.Abstractions;
using STRIDE.Modules.KitOps.Application.DTOs;

namespace STRIDE.Modules.KitOps.Infrastructure.ReadModels;

internal sealed class KitExcelExportService : IKitExcelExportService
{
    public byte[] ExportCheckoutHistory(IReadOnlyList<KitCheckoutReportRowDto> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Checkout History");

        var headers = new[]
        {
            "Kit Item", "Category", "Checked Out By", "User Email",
            "Checked Out At", "Expected Return", "Returned At",
            "Status", "Days Checked Out"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            cell.Style.Font.FontColor = XLColor.White;
        }

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var r = i + 2;
            ws.Cell(r, 1).Value = row.KitItemName;
            ws.Cell(r, 2).Value = row.Category;
            ws.Cell(r, 3).Value = row.CheckedOutByUserId.ToString();
            ws.Cell(r, 4).Value = row.CheckedOutByEmail ?? "";
            ws.Cell(r, 5).Value = row.CheckedOutAt;
            ws.Cell(r, 5).Style.NumberFormat.Format = "yyyy-mm-dd hh:mm";
            ws.Cell(r, 6).Value = row.ExpectedReturnAt;
            ws.Cell(r, 6).Style.NumberFormat.Format = "yyyy-mm-dd hh:mm";
            if (row.ReturnedAt.HasValue)
            {
                ws.Cell(r, 7).Value = row.ReturnedAt.Value;
                ws.Cell(r, 7).Style.NumberFormat.Format = "yyyy-mm-dd hh:mm";
            }
            ws.Cell(r, 8).Value = row.Status;
            ws.Cell(r, 9).Value = row.DaysCheckedOut;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportUsageSummary(IReadOnlyList<KitUsageSummaryRowDto> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Usage Summary");

        var headers = new[]
        {
            "Kit Item", "Category", "Total Quantity",
            "Total Checkouts", "Active", "Overdue",
            "Pending Reservations", "Avg Days Checked Out"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            cell.Style.Font.FontColor = XLColor.White;
        }

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var r = i + 2;
            ws.Cell(r, 1).Value = row.Name;
            ws.Cell(r, 2).Value = row.Category;
            ws.Cell(r, 3).Value = row.TotalQuantity;
            ws.Cell(r, 4).Value = row.TotalCheckouts;
            ws.Cell(r, 5).Value = row.ActiveCheckouts;
            ws.Cell(r, 6).Value = row.OverdueCheckouts;
            ws.Cell(r, 7).Value = row.PendingReservations;
            if (row.AvgDaysCheckedOut.HasValue)
            {
                ws.Cell(r, 8).Value = Math.Round(row.AvgDaysCheckedOut.Value, 1);
                ws.Cell(r, 8).Style.NumberFormat.Format = "0.0";
            }
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }
}
