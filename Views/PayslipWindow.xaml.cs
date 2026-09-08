using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using HR_ERP.Models;

namespace HR_ERP.Views
{
    public partial class PayslipWindow : Window
    {
        public PayslipWindow(PayrollRecord payroll, Employee? employee)
        {
            InitializeComponent();
            Viewer.Document = BuildDocument(payroll, employee);
        }

        private static FlowDocument BuildDocument(PayrollRecord p, Employee? e)
        {
            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 13,
                PagePadding = new Thickness(30)
            };

            // ---- Header ----
            var title = new Paragraph(new Run("HR ERP — Payslip"))
            {
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)new BrushConverter().ConvertFromString("#2C3E50")!,
                Margin = new Thickness(0, 0, 0, 2)
            };
            doc.Blocks.Add(title);

            var period = new Paragraph(new Run($"Pay Period: {p.PayPeriodStart:MMMM d, yyyy} – {p.PayPeriodEnd:MMMM d, yyyy}"))
            {
                FontSize = 13,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 16)
            };
            doc.Blocks.Add(period);

            // ---- Employee info table ----
            doc.Blocks.Add(SectionHeader("Employee Information"));
            var infoTable = NewTable(2);
            AddRow(infoTable, "Name", e?.Name ?? "(unknown)");
            AddRow(infoTable, "Employee Code", p.EmployeeCode);
            AddRow(infoTable, "Department", e?.Depart ?? "-");
            AddRow(infoTable, "Position", e?.Position ?? "-");
            AddRow(infoTable, "Employment Type", e?.EmploymentType ?? "-");
            doc.Blocks.Add(infoTable);

            // ---- Earnings ----
            doc.Blocks.Add(SectionHeader("Earnings"));
            var earnings = NewTable(2);
            AddRow(earnings, "Basic Salary", p.BasicSalary.ToString("N2"));
            AddRow(earnings, "Allowances", p.Allowances.ToString("N2"));
            AddRow(earnings, $"Overtime ({p.OvertimeHours:0.##} hrs)", p.OvertimeAmount.ToString("N2"));
            AddRow(earnings, "Gross Pay", (p.BasicSalary + p.Allowances + p.OvertimeAmount).ToString("N2"), bold: true);
            doc.Blocks.Add(earnings);

            // ---- Deductions ----
            doc.Blocks.Add(SectionHeader("Deductions"));
            var deductions = NewTable(2);
            AddRow(deductions, "Total Deductions", p.Deductions.ToString("N2"));
            doc.Blocks.Add(deductions);

            // ---- Net pay ----
            var netPara = new Paragraph()
            {
                Margin = new Thickness(0, 16, 0, 0),
                Background = (Brush)new BrushConverter().ConvertFromString("#E9F9EF")!,
                Padding = new Thickness(12, 10, 12, 10)
            };
            netPara.Inlines.Add(new Run("Net Salary: ") { FontWeight = FontWeights.Bold, FontSize = 15 });
            netPara.Inlines.Add(new Run(p.NetSalary.ToString("N2")) { FontWeight = FontWeights.Bold, FontSize = 15 });
            doc.Blocks.Add(netPara);

            // ---- Status / footer ----
            var statusPara = new Paragraph(new Run($"Status: {p.Status}" + (p.PaymentDate.HasValue ? $"   |   Paid on: {p.PaymentDate:d}" : "")))
            {
                Margin = new Thickness(0, 12, 0, 0),
                FontStyle = FontStyles.Italic,
                Foreground = Brushes.Gray
            };
            doc.Blocks.Add(statusPara);

            var footer = new Paragraph(new Run($"Generated {DateTime.Now:g} — HR ERP System"))
            {
                Margin = new Thickness(0, 30, 0, 0),
                FontSize = 10,
                Foreground = Brushes.Gray
            };
            doc.Blocks.Add(footer);

            return doc;
        }

        private static Paragraph SectionHeader(string text) => new(new Run(text))
        {
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            Foreground = (Brush)new BrushConverter().ConvertFromString("#2C3E50")!,
            Margin = new Thickness(0, 14, 0, 6),
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(0, 0, 0, 4)
        };

        private static Table NewTable(int columns)
        {
            var table = new Table { CellSpacing = 0, Margin = new Thickness(0) };
            for (int i = 0; i < columns; i++)
                table.Columns.Add(new TableColumn());
            table.RowGroups.Add(new TableRowGroup());
            return table;
        }

        private static void AddRow(Table table, string label, string value, bool bold = false)
        {
            var row = new TableRow();

            var labelCell = new TableCell(new Paragraph(new Run(label)) { Margin = new Thickness(0) })
            {
                Padding = new Thickness(4, 4, 4, 4),
                Foreground = Brushes.DimGray
            };
            var valueCell = new TableCell(new Paragraph(new Run(value)) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right })
            {
                Padding = new Thickness(4, 4, 4, 4),
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal
            };

            row.Cells.Add(labelCell);
            row.Cells.Add(valueCell);
            table.RowGroups[0].Rows.Add(row);
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Controls.PrintDialog();
            if (dialog.ShowDialog() != true) return;

            var paginator = ((IDocumentPaginatorSource)Viewer.Document).DocumentPaginator;
            dialog.PrintDocument(paginator, "Payslip");
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
