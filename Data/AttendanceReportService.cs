using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using ClosedXML.Excel;
using HR_ERP.Models;

namespace HR_ERP.Data
{
    public enum AttendanceReportType
    {
        DetailedLog,
        MonthlySummaryPerEmployee,
        DepartmentSummary
    }

    /// <summary>Builds attendance reports in two independent dimensions — which of the three
    /// report types (Detailed Log / per-employee summary / per-department summary), and which
    /// of the two output formats (a WPF FlowDocument ready for PrintDialog, or an .xlsx workbook
    /// via ClosedXML, the same library already used for the Excel import template) — so
    /// AttendanceReportWindow only has to gather the person's choices and hand them to whichever
    /// pair of methods matches.</summary>
    public static class AttendanceReportService
    {
        private class EmployeeSummaryRow
        {
            public string Code = "";
            public string Name = "";
            public string Department = "";
            public int Present, Absent, Late, OnLeave, Holiday, Weekend;
            public decimal TotalOvertimeHours;
            public int TotalLateMinutes;
        }

        private class DepartmentSummaryRow
        {
            public string Department = "";
            public int EmployeeCount;
            public int Present, Absent, Late, OnLeave;
            public decimal TotalOvertimeHours;
        }

        private static List<EmployeeSummaryRow> Summarize(List<AttendanceRecord> records)
        {
            return records
                .GroupBy(r => r.EmployeeCode)
                .Select(g =>
                {
                    var first = g.First();
                    return new EmployeeSummaryRow
                    {
                        Code = g.Key,
                        Name = first.Name ?? "",
                        Department = first.Depart ?? "",
                        Present = g.Count(r => string.Equals(r.Status1, "Present", StringComparison.OrdinalIgnoreCase)),
                        Absent = g.Count(r => string.Equals(r.Status1, "Absent", StringComparison.OrdinalIgnoreCase)),
                        Late = g.Count(r => string.Equals(r.Status1, "Late", StringComparison.OrdinalIgnoreCase)),
                        OnLeave = g.Count(r => string.Equals(r.Status1, "Leave", StringComparison.OrdinalIgnoreCase)),
                        Holiday = g.Count(r => string.Equals(r.Status1, "Holiday", StringComparison.OrdinalIgnoreCase)),
                        Weekend = g.Count(r => string.Equals(r.Status1, "Weekend", StringComparison.OrdinalIgnoreCase)),
                        TotalOvertimeHours = g.Sum(r => r.Overtime),
                        TotalLateMinutes = g.Sum(r => r.MinutesLate)
                    };
                })
                .OrderBy(r => r.Department).ThenBy(r => r.Name)
                .ToList();
        }

        private static List<DepartmentSummaryRow> SummarizeByDepartment(List<AttendanceRecord> records)
        {
            return records
                .GroupBy(r => r.Depart ?? "")
                .Select(g => new DepartmentSummaryRow
                {
                    Department = string.IsNullOrWhiteSpace(g.Key) ? "(No Department)" : g.Key,
                    EmployeeCount = g.Select(r => r.EmployeeCode).Distinct().Count(),
                    Present = g.Count(r => string.Equals(r.Status1, "Present", StringComparison.OrdinalIgnoreCase)),
                    Absent = g.Count(r => string.Equals(r.Status1, "Absent", StringComparison.OrdinalIgnoreCase)),
                    Late = g.Count(r => string.Equals(r.Status1, "Late", StringComparison.OrdinalIgnoreCase)),
                    OnLeave = g.Count(r => string.Equals(r.Status1, "Leave", StringComparison.OrdinalIgnoreCase)),
                    TotalOvertimeHours = g.Sum(r => r.Overtime)
                })
                .OrderBy(r => r.Department)
                .ToList();
        }

        // ==================================================== Print (FlowDocument) ====================================================

        public static FlowDocument BuildFlowDocument(AttendanceReportType type, List<AttendanceRecord> records,
            string companyTitle, string reportTitle, DateTime rangeStart, DateTime rangeEnd)
        {
            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                PagePadding = new Thickness(40),
                ColumnWidth = double.PositiveInfinity // single continuous column, not newspaper-style
            };

            doc.Blocks.Add(new Paragraph(new Run(companyTitle)) { FontSize = 18, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 2) });
            doc.Blocks.Add(new Paragraph(new Run(reportTitle)) { FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = Brushes.DimGray, Margin = new Thickness(0, 0, 0, 2) });
            doc.Blocks.Add(new Paragraph(new Run($"{rangeStart:d MMM yyyy} – {rangeEnd:d MMM yyyy}")) { FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0, 0, 0, 2) });
            doc.Blocks.Add(new Paragraph(new Run($"Generated {DateTime.Now:d MMM yyyy, HH:mm}")) { FontSize = 9, Foreground = Brushes.Gray, Margin = new Thickness(0, 0, 0, 12) });

            switch (type)
            {
                case AttendanceReportType.DetailedLog:
                    doc.Blocks.Add(BuildTable(
                        new[] { "Code", "Employee", "Date", "Day", "Check-in", "Check-out", "Shift", "Late (min)", "Early (min)", "Overtime", "Hours", "Status" },
                        records.Select(r => new object?[]
                        {
                            r.EmployeeCode, r.Name, r.CheckinDate?.ToString("d"), r.DayName,
                            r.Checkin?.ToString(@"hh\:mm"), r.Checkout?.ToString(@"hh\:mm"), r.ShiftName,
                            r.MinutesLate, r.MinutesEarlyLeave, r.Overtime, r.TWHours, r.Status1
                        })));
                    break;

                case AttendanceReportType.MonthlySummaryPerEmployee:
                    var empRows = Summarize(records);
                    doc.Blocks.Add(BuildTable(
                        new[] { "Code", "Employee", "Department", "Present", "Absent", "Late", "Leave", "Holiday", "Weekend", "Late (min)", "Overtime (hrs)" },
                        empRows.Select(r => new object?[]
                        {
                            r.Code, r.Name, r.Department, r.Present, r.Absent, r.Late, r.OnLeave, r.Holiday, r.Weekend,
                            r.TotalLateMinutes, r.TotalOvertimeHours
                        })));
                    break;

                case AttendanceReportType.DepartmentSummary:
                    var deptRows = SummarizeByDepartment(records);
                    doc.Blocks.Add(BuildTable(
                        new[] { "Department", "Employees", "Present", "Absent", "Late", "Leave", "Overtime (hrs)" },
                        deptRows.Select(r => new object?[]
                        {
                            r.Department, r.EmployeeCount, r.Present, r.Absent, r.Late, r.OnLeave, r.TotalOvertimeHours
                        })));
                    break;
            }

            return doc;
        }

        private static Table BuildTable(string[] headers, IEnumerable<object?[]> rows)
        {
            var table = new Table { CellSpacing = 0 };
            foreach (var _ in headers)
                table.Columns.Add(new TableColumn());

            var rowGroup = new TableRowGroup();
            table.RowGroups.Add(rowGroup);

            var headerRow = new TableRow { Background = Brushes.LightGray };
            foreach (var h in headers)
                headerRow.Cells.Add(MakeCell(h, bold: true));
            rowGroup.Rows.Add(headerRow);

            bool alt = false;
            foreach (var rowValues in rows)
            {
                var row = new TableRow { Background = alt ? Brushes.WhiteSmoke : Brushes.White };
                alt = !alt;
                foreach (var v in rowValues)
                    row.Cells.Add(MakeCell(v?.ToString() ?? ""));
                rowGroup.Rows.Add(row);
            }

            return table;
        }

        private static TableCell MakeCell(string text, bool bold = false)
        {
            var run = new Run(text);
            var para = new Paragraph(run) { FontSize = 10, FontWeight = bold ? FontWeights.Bold : FontWeights.Normal, Margin = new Thickness(0) };
            return new TableCell(para) { BorderBrush = Brushes.Gainsboro, BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(4, 3, 4, 3) };
        }

        // ==================================================== Excel export ====================================================

        public static void ExportToExcel(AttendanceReportType type, List<AttendanceRecord> records, string filePath,
            string reportTitle, DateTime rangeStart, DateTime rangeEnd)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Report");

            ws.Cell(1, 1).Value = reportTitle;
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(2, 1).Value = $"{rangeStart:yyyy-MM-dd} to {rangeEnd:yyyy-MM-dd}  —  generated {DateTime.Now:yyyy-MM-dd HH:mm}";
            ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

            int headerRow = 4;
            string[] headers;

            switch (type)
            {
                case AttendanceReportType.DetailedLog:
                    headers = new[] { "Code", "Employee", "Date", "Day", "Check-in", "Check-out", "Shift", "Late (min)", "Early (min)", "Overtime", "Hours", "Status" };
                    WriteHeaders(ws, headerRow, headers);
                    int r = headerRow + 1;
                    foreach (var rec in records)
                    {
                        ws.Cell(r, 1).Value = rec.EmployeeCode;
                        ws.Cell(r, 2).Value = rec.Name;
                        ws.Cell(r, 3).Value = rec.CheckinDate;
                        ws.Cell(r, 3).Style.DateFormat.Format = "yyyy-mm-dd";
                        ws.Cell(r, 4).Value = rec.DayName;
                        ws.Cell(r, 5).Value = rec.Checkin?.ToString(@"hh\:mm");
                        ws.Cell(r, 6).Value = rec.Checkout?.ToString(@"hh\:mm");
                        ws.Cell(r, 7).Value = rec.ShiftName;
                        ws.Cell(r, 8).Value = rec.MinutesLate;
                        ws.Cell(r, 9).Value = rec.MinutesEarlyLeave;
                        ws.Cell(r, 10).Value = rec.Overtime;
                        ws.Cell(r, 11).Value = rec.TWHours;
                        ws.Cell(r, 12).Value = rec.Status1;
                        r++;
                    }
                    break;

                case AttendanceReportType.MonthlySummaryPerEmployee:
                    headers = new[] { "Code", "Employee", "Department", "Present", "Absent", "Late", "Leave", "Holiday", "Weekend", "Late (min)", "Overtime (hrs)" };
                    WriteHeaders(ws, headerRow, headers);
                    int r2 = headerRow + 1;
                    foreach (var row in Summarize(records))
                    {
                        ws.Cell(r2, 1).Value = row.Code;
                        ws.Cell(r2, 2).Value = row.Name;
                        ws.Cell(r2, 3).Value = row.Department;
                        ws.Cell(r2, 4).Value = row.Present;
                        ws.Cell(r2, 5).Value = row.Absent;
                        ws.Cell(r2, 6).Value = row.Late;
                        ws.Cell(r2, 7).Value = row.OnLeave;
                        ws.Cell(r2, 8).Value = row.Holiday;
                        ws.Cell(r2, 9).Value = row.Weekend;
                        ws.Cell(r2, 10).Value = row.TotalLateMinutes;
                        ws.Cell(r2, 11).Value = row.TotalOvertimeHours;
                        r2++;
                    }
                    break;

                case AttendanceReportType.DepartmentSummary:
                default:
                    headers = new[] { "Department", "Employees", "Present", "Absent", "Late", "Leave", "Overtime (hrs)" };
                    WriteHeaders(ws, headerRow, headers);
                    int r3 = headerRow + 1;
                    foreach (var row in SummarizeByDepartment(records))
                    {
                        ws.Cell(r3, 1).Value = row.Department;
                        ws.Cell(r3, 2).Value = row.EmployeeCount;
                        ws.Cell(r3, 3).Value = row.Present;
                        ws.Cell(r3, 4).Value = row.Absent;
                        ws.Cell(r3, 5).Value = row.Late;
                        ws.Cell(r3, 6).Value = row.OnLeave;
                        ws.Cell(r3, 7).Value = row.TotalOvertimeHours;
                        r3++;
                    }
                    break;
            }

            ws.Row(headerRow).Style.Font.Bold = true;
            ws.Row(headerRow).Style.Fill.BackgroundColor = XLColor.LightGray;
            ws.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        private static void WriteHeaders(IXLWorksheet ws, int row, string[] headers)
        {
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(row, i + 1).Value = headers[i];
        }
    }
}
