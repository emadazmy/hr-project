using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using HR_ERP.Data;
using HR_ERP.Helpers;
using HR_ERP.Models;
using Microsoft.Win32;
using Localization = HR_ERP.Helpers.Localization;

namespace HR_ERP.Views
{
    /// <summary>Lets the person choose a report type (Detailed Log / per-employee summary /
    /// per-department summary), a scope (everyone / one employee / one department), a date
    /// range, and an output (Print via PrintDialog, or an .xlsx export) — then builds and
    /// delivers it via AttendanceReportService. Opened from AttendanceView's "Print Report"
    /// button.</summary>
    public partial class AttendanceReportWindow : Window
    {
        public AttendanceReportWindow()
        {
            InitializeComponent();
            ApplyLocalization();
            LoadLookups();
            SetThisMonthRange();
        }

        private void ApplyLocalization()
        {
            FlowDirection = Localization.FlowDirection;
            Title = Localization.T("Attendance.Report.Title");
            TitleText.Text = Localization.T("Attendance.Report.Title");

            ReportTypeLabel.Text = Localization.T("Attendance.Report.TypeLabel");
            TypeDetailedRadio.Content = Localization.T("Attendance.Report.Type.Detailed");
            TypeEmployeeSummaryRadio.Content = Localization.T("Attendance.Report.Type.EmployeeSummary");
            TypeDepartmentSummaryRadio.Content = Localization.T("Attendance.Report.Type.DepartmentSummary");

            ScopeLabel.Text = Localization.T("Attendance.Report.ScopeLabel");
            ScopeAllRadio.Content = Localization.T("Attendance.Report.Scope.All");
            ScopeEmployeeRadio.Content = Localization.T("Attendance.Report.Scope.Employee");
            ScopeDepartmentRadio.Content = Localization.T("Attendance.Report.Scope.Department");

            DateRangeLabel.Text = Localization.T("Attendance.Report.DateRangeLabel");
            ThisMonthButton.Content = Localization.T("Attendance.Button.ThisMonth");

            OutputLabel.Text = Localization.T("Attendance.Report.OutputLabel");
            OutputPrintRadio.Content = Localization.T("Attendance.Report.Output.Print");
            OutputExcelRadio.Content = Localization.T("Attendance.Report.Output.Excel");

            GenerateButton.Content = Localization.T("Attendance.Report.GenerateButton");
            CancelButton.Content = Localization.T("Common.Cancel");
        }

        private void LoadLookups()
        {
            try
            {
                ScopeEmployeeBox.ItemsSource = EmployeeRepository.GetAll();
                ScopeDepartmentBox.ItemsSource = DepartmentRepository.GetAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load employees/departments: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetThisMonthRange()
        {
            var today = DateTime.Today;
            RangeStartPicker.SelectedDate = new DateTime(today.Year, today.Month, 1);
            RangeEndPicker.SelectedDate = RangeStartPicker.SelectedDate.Value.AddMonths(1).AddDays(-1);
        }

        private void ThisMonth_Click(object sender, RoutedEventArgs e) => SetThisMonthRange();

        private void Scope_Changed(object sender, RoutedEventArgs e)
        {
            if (ScopeEmployeeBox == null || ScopeDepartmentBox == null) return; // still constructing
            ScopeEmployeeBox.IsEnabled = ScopeEmployeeRadio.IsChecked == true;
            ScopeDepartmentBox.IsEnabled = ScopeDepartmentRadio.IsChecked == true;
        }

        private AttendanceReportType SelectedReportType()
        {
            if (TypeEmployeeSummaryRadio.IsChecked == true) return AttendanceReportType.MonthlySummaryPerEmployee;
            if (TypeDepartmentSummaryRadio.IsChecked == true) return AttendanceReportType.DepartmentSummary;
            return AttendanceReportType.DetailedLog;
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            if (RangeStartPicker.SelectedDate == null || RangeEndPicker.SelectedDate == null
                || RangeEndPicker.SelectedDate < RangeStartPicker.SelectedDate)
            {
                MessageBox.Show(Localization.T("Attendance.Report.Validation.DateRange"), Localization.T("Common.Validation"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ScopeEmployeeRadio.IsChecked == true && ScopeEmployeeBox.SelectedItem == null)
            {
                MessageBox.Show(Localization.T("Attendance.Report.Validation.PickEmployee"), Localization.T("Common.Validation"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ScopeDepartmentRadio.IsChecked == true && ScopeDepartmentBox.SelectedItem == null)
            {
                MessageBox.Show(Localization.T("Attendance.Report.Validation.PickDepartment"), Localization.T("Common.Validation"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var start = RangeStartPicker.SelectedDate.Value;
            var end = RangeEndPicker.SelectedDate.Value;

            List<AttendanceRecord> records;
            try
            {
                records = AttendanceRepository.GetByDateRange(start, end);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load attendance data: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (ScopeEmployeeRadio.IsChecked == true && ScopeEmployeeBox.SelectedItem is Employee emp)
                records = records.Where(r => r.EmployeeCode == emp.Code).ToList();
            else if (ScopeDepartmentRadio.IsChecked == true && ScopeDepartmentBox.SelectedItem is Department dept)
                records = records.Where(r => string.Equals(r.Depart, dept.Name, StringComparison.OrdinalIgnoreCase)).ToList();

            var type = SelectedReportType();
            string reportTitle = type switch
            {
                AttendanceReportType.MonthlySummaryPerEmployee => Localization.T("Attendance.Report.Type.EmployeeSummary"),
                AttendanceReportType.DepartmentSummary => Localization.T("Attendance.Report.Type.DepartmentSummary"),
                _ => Localization.T("Attendance.Report.Type.Detailed")
            };

            try
            {
                if (OutputPrintRadio.IsChecked == true)
                {
                    var doc = AttendanceReportService.BuildFlowDocument(type, records, Localization.T("AppTitle"), reportTitle, start, end);
                    var printDialog = new System.Windows.Controls.PrintDialog();
                    if (printDialog.ShowDialog() == true)
                    {
                        IDocumentPaginatorSource paginatorSource = doc;
                        printDialog.PrintDocument(paginatorSource.DocumentPaginator, reportTitle);
                        MessageBox.Show(Localization.T("Attendance.Report.Sent"), Localization.T("Common.Info"), MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    var dialog = new SaveFileDialog
                    {
                        Title = Localization.T("Attendance.Report.SaveExcelTitle"),
                        Filter = Localization.T("Attendance.Dialog.Filter.ExcelFiles"),
                        FileName = $"Attendance_Report_{start:yyyyMMdd}_{end:yyyyMMdd}.xlsx"
                    };
                    if (dialog.ShowDialog() != true) return;

                    AttendanceReportService.ExportToExcel(type, records, dialog.FileName, reportTitle, start, end);
                    MessageBox.Show(Localization.T("Attendance.Report.ExcelSaved"), Localization.T("Common.Done"), MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Localization.T("Attendance.Report.Error"), ex.Message), Localization.T("Common.Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
