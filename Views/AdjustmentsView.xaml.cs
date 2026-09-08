using System.Windows;
using System.Windows.Controls;
using HR_ERP.Data;
using HR_ERP.Helpers;
using HR_ERP.Models;
using Localization = HR_ERP.Helpers.Localization;

namespace HR_ERP.Views
{
    public partial class AdjustmentsView : UserControl
    {
        private PayrollAdjustment? _selected;

        public AdjustmentsView()
        {
            InitializeComponent();
            ApplyLocalization();
            LoadMonths();
            LoadEmployees();
            LoadAdjustments();
        }

        /// <summary>Translates every label, header and button on this screen. The Type data
        /// value ("Bonus"/"Incentive"/"Penalty") is deliberately left in English regardless of
        /// language — it's stored as literal text and matched against elsewhere (the row-color
        /// triggers just above, and PayrollGenerationService when rolling adjustments into
        /// Allowances/Deductions), so translating the displayed text would silently break
        /// those comparisons.</summary>
        private void ApplyLocalization()
        {
            TitleText.Text = Localization.T("Adjustments.Title");
            MonthLabel.Content = Localization.T("Adjustments.Field.Month");

            ColCode.Header = Localization.T("Adjustments.Col.Code");
            ColEmployee.Header = Localization.T("Adjustments.Col.Employee");
            ColType.Header = Localization.T("Adjustments.Col.Type");
            ColAmount.Header = Localization.T("Adjustments.Col.Amount");
            ColDescription.Header = Localization.T("Adjustments.Col.Description");
            ColCreated.Header = Localization.T("Adjustments.Col.Created");

            EmployeeLabel.Content = Localization.T("Adjustments.Field.Employee");
            TypeLabel.Content = Localization.T("Adjustments.Field.Type");
            AmountLabel.Content = Localization.T("Adjustments.Field.Amount");
            DescriptionLabel.Content = Localization.T("Adjustments.Field.Description");
            NoteText.Text = Localization.T("Adjustments.Note");

            AddButton.Content = Localization.T("Adjustments.Button.Add");
            DeleteButton.Content = Localization.T("Adjustments.Button.Delete");

            if (TypeBox != null)
            {
                var items = TypeBox.Items.OfType<ComboBoxItem>().ToList();
                if (items.Count >= 3)
                {
                    items[0].Content = Localization.T("Adjustments.type.Bonus"); items[0].Tag = "Bonus";
                    items[1].Content = Localization.T("Adjustments.type.Incentive"); items[1].Tag = "Incentive";
                    items[2].Content = Localization.T("Adjustments.type.Penalty"); items[2].Tag = "Penalty";
                }
            }
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
            MonthBox.SelectedIndex = 0;
        }

        private void LoadEmployees()
        {
            try { EmployeeBox.ItemsSource = EmployeeRepository.GetAll(); }
            catch (Exception ex) { MessageBox.Show("Could not load employees: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void LoadAdjustments()
        {
            if (MonthBox.SelectedItem is not string month) return;
            AdjustmentsGrid.ItemsSource = PayrollAdjustmentRepository.GetByMonth(month);
        }

        private void MonthBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadAdjustments();

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeeBox.SelectedItem is not Employee emp || TypeBox.SelectedItem is not ComboBoxItem type
                || MonthBox.SelectedItem is not string month)
            {
                MessageBox.Show("Employee, type and month are required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(AmountBox.Text, out var amount) || amount <= 0)
            {
                MessageBox.Show("Amount must be a positive number.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                PayrollAdjustmentRepository.Add(new PayrollAdjustment
                {
                    EmployeeCode = emp.Code,
                    Type = type.Tag?.ToString() ?? "Bonus",
                    Month = month,
                    Amount = amount,
                    Description = DescriptionBox.Text.Trim()
                });

                AmountBox.Text = "0";
                DescriptionBox.Text = "";
                LoadAdjustments();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Add failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (AdjustmentsGrid.SelectedItem is not PayrollAdjustment adj)
            {
                MessageBox.Show("Select a row first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"Delete this {adj.Type.ToLower()} of {adj.Amount:N2} for {adj.EmployeeName}?",
                "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            try
            {
                PayrollAdjustmentRepository.Delete(adj.Id);
                LoadAdjustments();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Delete failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
