using System.Windows;
using System.Windows.Controls;
using HR_ERP.Data;
using HR_ERP.Models;
using Localization = HR_ERP.Helpers.Localization;

namespace HR_ERP.Views
{
    public partial class DepartmentsView : UserControl
    {
        private Department? _selectedDept;
        private JobPosition? _selectedPos;
        private List<Department> _departments = new();

        public DepartmentsView()
        {
            InitializeComponent();
            LoadDepartments();
            LoadPositions();
            Localization.LanguageChanged += ApplyLocalization;
            this.Unloaded += (s, e) => Localization.LanguageChanged -= ApplyLocalization;
            ApplyLocalization();
        }

        private void LoadDepartments()
        {
            try
            {
                _departments = DepartmentRepository.GetAll();
                DepartmentsGrid.ItemsSource = _departments;
                PosDeptBox.ItemsSource = _departments;
                ParentDeptBox.ItemsSource = _departments;
            }
            catch (Exception ex)
            {
                MessageBox.Show(Localization.T("Departments.Error.LoadDepartments") + ": " + ex.Message, Localization.T("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPositions(string? departmentName = null)
        {
            PositionsGrid.ItemsSource = string.IsNullOrWhiteSpace(departmentName)
                ? JobPositionRepository.GetAll()
                : JobPositionRepository.GetByDepartment(departmentName);
        }

        private void DepartmentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DepartmentsGrid.SelectedItem is not Department d) return;
            _selectedDept = d;
            DeptCodeBox.Text = d.Code;
            DeptNameBox.Text = d.Name;
            DeptTypeBox.Text = d.Type;
            ParentDeptBox.ItemsSource = _departments.Where(x => x.Code != d.Code).ToList(); // can't be its own parent
            ParentDeptBox.SelectedItem = _departments.FirstOrDefault(x => x.Code == d.ParentCode);

            // Narrow the Positions grid to just this department, and default the
            // new-position form to it too, for convenience.
            LoadPositions(d.Name);
            PosNew_Click(sender, e);
            PosDeptBox.SelectedItem = d;
        }

        private void DeptNew_Click(object sender, RoutedEventArgs e)
        {
            _selectedDept = null;
            DeptCodeBox.Text = DeptNameBox.Text = DeptTypeBox.Text = "";
            ParentDeptBox.ItemsSource = _departments;
            ParentDeptBox.SelectedItem = null;
            DepartmentsGrid.SelectedItem = null;
            LoadPositions(); // no department selected -> show all positions again
        }

        private void DeptSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DeptCodeBox.Text) || string.IsNullOrWhiteSpace(DeptNameBox.Text))
            {
                MessageBox.Show(Localization.T("Departments.Validation.CodeAndNameRequired"), Localization.T("Common.Validation"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var parentCode = (ParentDeptBox.SelectedItem as Department)?.Code;

            if (!string.IsNullOrWhiteSpace(parentCode) && _selectedDept != null)
            {
                // Guard against creating a cycle (X's parent is Y, whose parent is X) —
                // only checks one level deep since this UI only supports two levels
                // (main department + sub-department), matching the org structure view.
                var parentDept = _departments.FirstOrDefault(d => d.Code == parentCode);
                if (parentDept?.ParentCode == _selectedDept.Code)
                {
                    MessageBox.Show(Localization.T("Departments.Validation.CircularParent"), Localization.T("Common.Validation"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var dept = new Department
            {
                Id = _selectedDept?.Id ?? 0,
                Code = DeptCodeBox.Text.Trim(),
                Name = DeptNameBox.Text.Trim(),
                Type = DeptTypeBox.Text.Trim(),
                ParentCode = parentCode
            };

            try
            {
                if (_selectedDept == null) DepartmentRepository.Add(dept);
                else DepartmentRepository.Update(dept);

                LoadDepartments();
                DeptNew_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Localization.T("Departments.Error.SaveFailed") + ": " + ex.Message, Localization.T("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeptDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDept == null) return;
            var confirmDeptMsg = string.Format(Localization.T("Departments.Delete.Confirm"), _selectedDept.Name);
            if (MessageBox.Show(confirmDeptMsg, Localization.T("Common.Confirm"), MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            try
            {
                DepartmentRepository.Delete(_selectedDept.Id);
                LoadDepartments();
                DeptNew_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Localization.T("Departments.Error.DeleteFailed") + ": " + ex.Message, Localization.T("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PositionsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PositionsGrid.SelectedItem is not JobPosition p) return;
            _selectedPos = p;
            PosCodeBox.Text = p.Code;
            PosTitleBox.Text = p.Title;
            PosDescBox.Text = p.JobDescription;
            PosDeptBox.SelectedItem = _departments.FirstOrDefault(d => d.Name == p.Depart);
        }

        private void PosNew_Click(object sender, RoutedEventArgs e)
        {
            _selectedPos = null;
            PosCodeBox.Text = PosTitleBox.Text = PosDescBox.Text = "";
            PosDeptBox.SelectedItem = null;
            PositionsGrid.SelectedItem = null;
        }

        private void PosSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PosTitleBox.Text) || PosDeptBox.SelectedItem == null)
            {
                MessageBox.Show(Localization.T("Departments.Validation.PositionTitleRequired"), Localization.T("Common.Validation"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var pos = new JobPosition
            {
                Id = _selectedPos?.Id ?? 0,
                Code = PosCodeBox.Text.Trim(),
                Title = PosTitleBox.Text.Trim(),
                Depart = ((Department)PosDeptBox.SelectedItem).Name,
                JobDescription = PosDescBox.Text.Trim()
            };

            try
            {
                if (_selectedPos == null) JobPositionRepository.Add(pos);
                else JobPositionRepository.Update(pos);

                LoadPositions(_selectedDept?.Name);
                PosNew_Click(sender, e);
                if (_selectedDept != null) PosDeptBox.SelectedItem = _selectedDept;
            }
            catch (Exception ex)
            {
                MessageBox.Show(Localization.T("Departments.Error.PositionSaveFailed") + ": " + ex.Message, Localization.T("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PosDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPos == null) return;
            if (MessageBox.Show(string.Format(Localization.T("Positions.Delete.Confirm"), _selectedPos.Title), Localization.T("Common.Confirm"), MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            try
            {
                JobPositionRepository.Delete(_selectedPos.Id);
                LoadPositions(_selectedDept?.Name);
                PosNew_Click(sender, e);
                if (_selectedDept != null) PosDeptBox.SelectedItem = _selectedDept;
            }
            catch (Exception ex)
            {
                MessageBox.Show(Localization.T("Departments.Error.PositionDeleteFailed") + ": " + ex.Message, Localization.T("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyLocalization()
        {
            this.FlowDirection = Localization.FlowDirection;
            DepartmentsHeader.Text = Localization.T("Departments.Header");
            DeptColCode.Header = Localization.T("Departments.Column.Code");
            DeptColName.Header = Localization.T("Departments.Column.Name");
            DeptColParent.Header = Localization.T("Departments.Column.Parent");
            DeptColType.Header = Localization.T("Departments.Column.Type");

            DeptCodeLabel.Content = Localization.T("Departments.Label.Code");
            DeptNameLabel.Content = Localization.T("Departments.Label.Name");
            DeptTypeLabel.Content = Localization.T("Departments.Label.Type");
            ParentDeptLabel.Content = Localization.T("Departments.Label.ParentHint");
            DeptNewButton.Content = Localization.T("Departments.Button.New");
            DeptSaveButton.Content = Localization.T("Departments.Button.Save");
            DeptDeleteButton.Content = Localization.T("Departments.Button.Delete");

            PositionsHeader.Text = Localization.T("Positions.Header");
            PositionsDesc.Text = Localization.T("Positions.Description");
            PosColCode.Header = Localization.T("Positions.Column.Code");
            PosColTitle.Header = Localization.T("Positions.Column.Title");
            PosColDepartment.Header = Localization.T("Positions.Column.Department");

            PosCodeLabel.Content = Localization.T("Positions.Label.Code");
            PosTitleLabel.Content = Localization.T("Positions.Label.Title");
            PosDeptLabel.Content = Localization.T("Positions.Label.Department");
            PosDescLabel.Content = Localization.T("Positions.Label.Description");
            PosNewButton.Content = Localization.T("Departments.Button.New");
            PosSaveButton.Content = Localization.T("Departments.Button.Save");
            PosDeleteButton.Content = Localization.T("Departments.Button.Delete");
        }
    }
}
