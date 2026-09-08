using System.Text;
using System.Windows;
using System.Windows.Controls;
using HR_ERP.Data;
using HR_ERP.Helpers;
using HR_ERP.Models;
using Localization = HR_ERP.Helpers.Localization;

namespace HR_ERP.Views
{
    public partial class PayrollView : UserControl
    {
        private PayrollRecord? _selected;

        public PayrollView()
        {
            InitializeComponent();
            ApplyLocalization();
            LoadMonths();
            LoadEmployees();
            LoadPayroll();
        }

        /// <summary>Translates every label, header and button on this screen. The Status data
        /// value ("Paid"/"Pending") is deliberately left in English regardless of language —
        /// it's stored as literal text and matched against elsewhere (PayrollRepository's
        /// "don't overwrite an already-paid record" check, PayrollGenerationService, and the
        /// row-color trigger just above), so translating the displayed text would silently
        /// break those comparisons.</summary>
        private void ApplyLocalization()
        {
            TitleText.Text = Localization.T("Payroll.Title");
            MonthLabel.Content = Localization.T("Payroll.Field.Month");
            AutoGenerateButton.Content = Localization.T("Payroll.Button.AutoGenerate");

            ColCode.Header = Localization.T("Payroll.Col.Code");
            ColEmployee.Header = Localization.T("Payroll.Col.Employee");
            ColPeriodStart.Header = Localization.T("Payroll.Col.PeriodStart");
            ColPeriodEnd.Header = Localization.T("Payroll.Col.PeriodEnd");
            ColBasic.Header = Localization.T("Payroll.Col.Basic");
            ColAllowances.Header = Localization.T("Payroll.Col.Allowances");
            ColDeductions.Header = Localization.T("Payroll.Col.Deductions");
            ColOtAmount.Header = Localization.T("Payroll.Col.OtAmount");
            ColNetSalary.Header = Localization.T("Payroll.Col.NetSalary");
            ColStatus.Header = Localization.T("Payroll.Col.Status");

            EmployeeLabel.Content = Localization.T("Payroll.Field.Employee");
            PeriodStartLabel.Content = Localization.T("Payroll.Field.PeriodStart");
            PeriodEndLabel.Content = Localization.T("Payroll.Field.PeriodEnd");
            BasicSalaryLabel.Content = Localization.T("Payroll.Field.BasicSalary");
            AllowancesLabel.Content = Localization.T("Payroll.Field.Allowances");
            DeductionsLabel.Content = Localization.T("Payroll.Field.Deductions");
            OvertimeHoursLabel.Content = Localization.T("Payroll.Field.OvertimeHours");
            OvertimeAmountLabel.Content = Localization.T("Payroll.Field.OvertimeAmount");

            NewButton.Content = Localization.T("Payroll.Button.New");
            SaveManualButton.Content = Localization.T("Payroll.Button.SaveManual");
            MarkPaidButton.Content = Localization.T("Payroll.Button.MarkPaid");
            PrintPayslipButton.Content = Localization.T("Payroll.Button.PrintPayslip");
            DeleteButton.Content = Localization.T("Payroll.Button.Delete");
        }

        private void LoadMonths()
        {
            var months = new List<string>();
            var cursor = DateTime.Today;
            for (int i = 0; i < 12; i++)
            {
                months.Add(cursor.ToString("yyyy-MM"));
                cursor = cursor.AddMonths(-1);
            }
            MonthBox.ItemsSource = months;
            MonthBox.SelectedIndex = 0; // current month
        }

        private void LoadEmployees()
        {
            try
            {
                EmployeeBox.ItemsSource = EmployeeRepository.GetAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load employees: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPayroll() => PayrollGrid.ItemsSource = PayrollRepository.GetAll();

        private void EmployeeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Pre-fill the basic salary from the employee's job position, as a convenience.
            if (EmployeeBox.SelectedItem is Employee emp && !string.IsNullOrWhiteSpace(emp.Position) && _selected == null)
            {
                // Base salary isn't stored on JobPositions in this schema anymore, so this is
                // left blank for manual entry; wire it up if you add a salary column back.
            }
        }

        private void PayrollGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PayrollGrid.SelectedItem is not PayrollRecord p) return;
            _selected = p;

            EmployeeBox.SelectedItem = (EmployeeBox.ItemsSource as List<Employee>)?.FirstOrDefault(x => x.Code == p.EmployeeCode);
            PeriodStartPicker.SelectedDate = p.PayPeriodStart;
            PeriodEndPicker.SelectedDate = p.PayPeriodEnd;
            BasicSalaryBox.Text = p.BasicSalary.ToString();
            AllowancesBox.Text = p.Allowances.ToString();
            DeductionsBox.Text = p.Deductions.ToString();
            OvertimeHoursBox.Text = p.OvertimeHours.ToString();
            OvertimeAmountBox.Text = p.OvertimeAmount.ToString();
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _selected = null;
            EmployeeBox.SelectedItem = null;
            PeriodStartPicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            PeriodEndPicker.SelectedDate = DateTime.Today;
            BasicSalaryBox.Text = "0";
            AllowancesBox.Text = "0";
            DeductionsBox.Text = "0";
            OvertimeHoursBox.Text = "0";
            OvertimeAmountBox.Text = "0";
            PayrollGrid.SelectedItem = null;
        }

        private static decimal ParseDecimal(string text) => decimal.TryParse(text, out var val) ? val : 0m;

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeeBox.SelectedItem is not Employee emp || PeriodStartPicker.SelectedDate == null || PeriodEndPicker.SelectedDate == null)
            {
                MessageBox.Show("Employee, period start and period end are required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var record = new PayrollRecord
            {
                EmployeeCode = emp.Code,
                PayPeriodStart = PeriodStartPicker.SelectedDate.Value,
                PayPeriodEnd = PeriodEndPicker.SelectedDate.Value,
                BasicSalary = ParseDecimal(BasicSalaryBox.Text),
                Allowances = ParseDecimal(AllowancesBox.Text),
                Deductions = ParseDecimal(DeductionsBox.Text),
                OvertimeHours = ParseDecimal(OvertimeHoursBox.Text),
                OvertimeAmount = ParseDecimal(OvertimeAmountBox.Text),
                Status = "Pending"
            };

            try
            {
                PayrollRepository.Add(record);
                LoadPayroll();
                New_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Generate failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MarkPaid_Click(object sender, RoutedEventArgs e)
        {
            if (_selected == null)
            {
                MessageBox.Show("Select a payroll record first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                PayrollRepository.MarkPaid(_selected.PayrollID, DateTime.Today);
                LoadPayroll();
                New_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Update failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_selected == null) return;
            if (MessageBox.Show("Delete this payroll record?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            try
            {
                PayrollRepository.Delete(_selected.PayrollID);
                LoadPayroll();
                New_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Delete failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AutoGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (MonthBox.SelectedItem is not string month)
            {
                MessageBox.Show("Select a month first.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show(
                    $"This will recompute attendance totals for {month} from the Attendance table and " +
                    "generate/update a payroll row for every active employee. Continue?",
                    "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                var result = PayrollGenerationService.GenerateForMonth(month);
                LoadPayroll();

                var summary = new StringBuilder();
                summary.AppendLine($"Payroll generated/updated for {result.Generated} employee(s).");
                if (result.Skipped.Count > 0)
                {
                    summary.AppendLine($"Skipped {result.Skipped.Count}:");
                    foreach (var s in result.Skipped.Take(15))
                        summary.AppendLine($"  {s.EmployeeCode} ({s.EmployeeName}): {s.Message}");
                }

                MessageBox.Show(summary.ToString(), "Auto-Generate Complete", MessageBoxButton.OK,
                    result.Skipped.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Auto-generate failed: " + ex.Message +
                    "\n\nMake sure sp_GenerateMonthlyAttendanceSummary has been created in the database " +
                    "(Database/sp_GenerateMonthlyAttendanceSummary.sql).",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void PrintPayslip_Click(object sender, RoutedEventArgs e)
        {
            if (_selected == null)
            {
                MessageBox.Show("Select a payroll record first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var employee = EmployeeRepository.GetByCode(_selected.EmployeeCode);
                var window = new PayslipWindow(_selected, employee) { Owner = Window.GetWindow(this) };
                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open payslip: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
