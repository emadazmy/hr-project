using System.Windows;
using System.Windows.Controls;
using HR_ERP.Data;
using HR_ERP.Helpers;
using HR_ERP.Models;
using Localization = HR_ERP.Helpers.Localization;

namespace HR_ERP.Views
{
    /// <summary>The employee list page. Editing (Add/Edit, Documents, History, Salary Details)
    /// happens in a separate EmployeeDetailWindow opened by New/Edit — this page's only job is
    /// showing, searching/filtering, and picking an employee, so the DataGrid always has the
    /// full page to itself instead of competing with expanding edit panels for space.</summary>
    public partial class EmployeesView : UserControl
    {
        private List<Department> _departments = new();
        private Employee? _selected;

        public EmployeesView()
        {
            InitializeComponent();
            Localization.LanguageChanged += OnLanguageChanged;
            this.Unloaded += (_, _) => Localization.LanguageChanged -= OnLanguageChanged;
            ApplyLocalization();

            LoadDepartments();
            LoadEmployees();
        }

        private void OnLanguageChanged() => ApplyLocalization();

        private void ApplyLocalization()
        {
            this.FlowDirection = Localization.FlowDirection;

            HeaderText.Text = Localization.T("Employees.Header");
            SearchButton.Content = Localization.T("Employees.Search");
            RefreshButton.Content = Localization.T("Employees.Refresh");
            DepartmentLabel.Content = Localization.T("Employees.Filter.Department");

            ColCode.Header = Localization.T("Employees.Column.Code");
            ColName.Header = Localization.T("Employees.Column.Name");
            ColDepartment.Header = Localization.T("Employees.Column.Department");
            ColPosition.Header = Localization.T("Employees.Column.Position");
            ColEmail.Header = Localization.T("Employees.Column.Email");
            ColPhone.Header = Localization.T("Employees.Column.Phone");
            ColHireDate.Header = Localization.T("Employees.Column.HireDate");
            ColStatus.Header = Localization.T("Employees.Column.Status");

            NewButton.Content = Localization.T("Employees.Button.New");
            EditButton.Content = Localization.T("Employees.Button.Edit");
            DeleteButton.Content = Localization.T("Employees.Button.Delete");

            if (FilterDepartmentBox?.Items != null && FilterDepartmentBox.Items.Count > 0)
            {
                var first = FilterDepartmentBox.Items[0] as Department;
                if (first != null) first.Name = Localization.T("Employees.Filter.AllDepartments");
            }
        }

        private void LoadDepartments()
        {
            _departments = DepartmentRepository.GetAll();

            var filterList = new List<Department> { new() { Id = 0, Name = Localization.T("Employees.Filter.AllDepartments") } };
            filterList.AddRange(_departments);
            FilterDepartmentBox.ItemsSource = filterList;
            FilterDepartmentBox.SelectedIndex = 0;
        }

        private void LoadEmployees(string? search = null)
        {
            try
            {
                var employees = EmployeeRepository.GetAll(search);

                if (FilterDepartmentBox.SelectedItem is Department filterDept && filterDept.Id != 0)
                    employees = employees.Where(emp => string.Equals(emp.Depart, filterDept.Name, StringComparison.OrdinalIgnoreCase)).ToList();

                EmployeesGrid.ItemsSource = employees;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Localization.T("Employees.Error.CouldNotLoad"), ex.Message),
                    Localization.T("Attendance.Error.Title"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Search_Click(object sender, RoutedEventArgs e) => LoadEmployees(SearchBox.Text);
        private void Refresh_Click(object sender, RoutedEventArgs e) { SearchBox.Text = ""; LoadEmployees(); }
        private void FilterDepartmentBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadEmployees(SearchBox.Text);

        private void EmployeesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selected = EmployeesGrid.SelectedItem as Employee;
            EditButton.IsEnabled = _selected != null;
            DeleteButton.IsEnabled = _selected != null;
        }

        private void EmployeesGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (EmployeesGrid.SelectedItem is Employee) Edit_Click(sender, new RoutedEventArgs());
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            var window = new EmployeeDetailWindow(null) { Owner = Window.GetWindow(this) };
            window.ShowDialog();
            if (window.Saved) LoadEmployees(SearchBox.Text);
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (_selected == null) return;
            var window = new EmployeeDetailWindow(_selected) { Owner = Window.GetWindow(this) };
            window.ShowDialog();
            if (window.Saved) LoadEmployees(SearchBox.Text);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_selected == null) return;

            if (MessageBox.Show($"Delete {_selected.Name}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                EmployeeRepository.Delete(_selected.Id);
                _selected = null;
                EditButton.IsEnabled = false;
                DeleteButton.IsEnabled = false;
                LoadEmployees(SearchBox.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Delete failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
