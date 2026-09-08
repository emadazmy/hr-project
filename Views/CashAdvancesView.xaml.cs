using System.Windows;
using System.Windows.Controls;
using HR_ERP.Data;
using HR_ERP.Helpers;
using HR_ERP.Models;
using Localization = HR_ERP.Helpers.Localization;

namespace HR_ERP.Views
{
    public partial class CashAdvancesView : UserControl
    {
        private CashAdvance? _selected;

        public CashAdvancesView()
        {
            InitializeComponent();
            ApplyLocalization();
            LoadMonths();
            LoadEmployees();
            LoadAdvances();
        }

        /// <summary>Translates every label, header and button on this screen. The Status data
        /// value ("Active"/"Completed") is deliberately left in English regardless of language —
        /// it's stored as literal text and matched against elsewhere (the row-color trigger just
        /// above and CashAdvanceRepository.UpdateStatus), so translating the displayed text
        /// would silently break those comparisons.</summary>
        private void ApplyLocalization()
        {
            TitleText.Text = Localization.T("CashAdvances.Title");
            AsOfLabel.Content = Localization.T("CashAdvances.Field.AsOf");

            ColCode.Header = Localization.T("CashAdvances.Col.Code");
            ColEmployee.Header = Localization.T("CashAdvances.Col.Employee");
            ColAmount.Header = Localization.T("CashAdvances.Col.Amount");
            ColInstallments.Header = Localization.T("CashAdvances.Col.Installments");
            ColMonthly.Header = Localization.T("CashAdvances.Col.Monthly");
            ColStartMonth.Header = Localization.T("CashAdvances.Col.StartMonth");
            ColPaidSoFar.Header = Localization.T("CashAdvances.Col.PaidSoFar");
            ColRemaining.Header = Localization.T("CashAdvances.Col.Remaining");
            ColStatus.Header = Localization.T("CashAdvances.Col.Status");

            EmployeeLabel.Content = Localization.T("CashAdvances.Field.Employee");
            TotalAmountLabel.Content = Localization.T("CashAdvances.Field.TotalAmount");
            InstallmentsLabel.Content = Localization.T("CashAdvances.Field.Installments");
            MonthlyAutoLabel.Content = Localization.T("CashAdvances.Field.MonthlyAuto");
            StartMonthLabel.Content = Localization.T("CashAdvances.Field.StartMonth");
            NotesLabel.Content = Localization.T("CashAdvances.Field.Notes");
            AutoDeductNoteText.Text = Localization.T("CashAdvances.AutoDeductNote");

            NewButton.Content = Localization.T("CashAdvances.Button.New");
            AddAdvanceButton.Content = Localization.T("CashAdvances.Button.Add");
            MarkCompletedButton.Content = Localization.T("CashAdvances.Button.MarkCompleted");
            DeleteButton.Content = Localization.T("CashAdvances.Button.Delete");
        }

        private void LoadMonths()
        {
            var months = new List<string>();
            var cursor = DateTime.Today.AddMonths(2); // include a couple of future months for start-month picking
            for (int i = 0; i < 18; i++)
            {
                months.Add(cursor.ToString("yyyy-MM"));
                cursor = cursor.AddMonths(-1);
            }
            AsOfMonthBox.ItemsSource = months;
            AsOfMonthBox.SelectedItem = DateTime.Today.ToString("yyyy-MM");
            StartMonthBox.ItemsSource = months;
            StartMonthBox.SelectedItem = DateTime.Today.ToString("yyyy-MM");
        }

        private void LoadEmployees()
        {
            try { EmployeeBox.ItemsSource = EmployeeRepository.GetAll(); }
            catch (Exception ex) { MessageBox.Show("Could not load employees: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void LoadAdvances()
        {
            if (AsOfMonthBox.SelectedItem is not string asOf) asOf = DateTime.Today.ToString("yyyy-MM");

            try
            {
                var advances = CashAdvanceRepository.GetAll();
                AdvancesGrid.ItemsSource = advances.Select(a => new CashAdvanceRow(a, asOf)).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load cash advances: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AsOfMonthBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadAdvances();

        private void AdvancesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => _selected = (AdvancesGrid.SelectedItem as CashAdvanceRow)?.Source;

        private void RecalculateMonthly_LostFocus(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(AmountBox.Text, out var amount) && int.TryParse(InstallmentsBox.Text, out var installments) && installments > 0)
                MonthlyBox.Text = Math.Round(amount / installments, 2).ToString();
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _selected = null;
            EmployeeBox.SelectedItem = null;
            AmountBox.Text = "0";
            InstallmentsBox.Text = "1";
            MonthlyBox.Text = "0";
            StartMonthBox.SelectedItem = DateTime.Today.ToString("yyyy-MM");
            NotesBox.Text = "";
            AdvancesGrid.SelectedItem = null;
        }

        private void AddAdvance_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeeBox.SelectedItem is not Employee emp || StartMonthBox.SelectedItem is not string startMonth)
            {
                MessageBox.Show("Employee and start month are required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(AmountBox.Text, out var amount) || amount <= 0)
            {
                MessageBox.Show("Total amount must be a positive number.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(InstallmentsBox.Text, out var installments) || installments <= 0)
            {
                MessageBox.Show("Installments must be a positive whole number.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                CashAdvanceRepository.Add(new CashAdvance
                {
                    EmployeeCode = emp.Code,
                    Amount = amount,
                    Installments = installments,
                    MonthlyDeduction = Math.Round(amount / installments, 2),
                    StartMonth = startMonth,
                    Status = "Active",
                    Notes = NotesBox.Text.Trim()
                });

                LoadAdvances();
                New_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Add failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MarkCompleted_Click(object sender, RoutedEventArgs e)
        {
            if (_selected == null)
            {
                MessageBox.Show("Select an advance first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                CashAdvanceRepository.UpdateStatus(_selected.Id, "Completed");
                LoadAdvances();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Update failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_selected == null)
            {
                MessageBox.Show("Select an advance first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"Delete this cash advance for {_selected.EmployeeName}? This does not reverse any payroll deductions already applied.",
                "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            try
            {
                CashAdvanceRepository.Delete(_selected.Id);
                LoadAdvances();
                New_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Delete failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>Wraps a CashAdvance with Paid/Remaining computed as of the selected month,
        /// for grid display (the model's PaidAsOf/RemainingAsOf need a month parameter, so
        /// they can't be bound directly).</summary>
        private class CashAdvanceRow
        {
            public CashAdvance Source { get; }
            public string EmployeeCode => Source.EmployeeCode;
            public string? EmployeeName => Source.EmployeeName;
            public decimal Amount => Source.Amount;
            public int Installments => Source.Installments;
            public decimal MonthlyDeduction => Source.MonthlyDeduction;
            public string StartMonth => Source.StartMonth;
            public string Status => Source.Status;
            public string PaidDisplay { get; }
            public string RemainingDisplay { get; }

            public CashAdvanceRow(CashAdvance source, string asOfMonth)
            {
                Source = source;
                PaidDisplay = source.PaidAsOf(asOfMonth).ToString("N2");
                RemainingDisplay = source.RemainingAsOf(asOfMonth).ToString("N2");
            }
        }
    }
}
