using System.Windows;
using System.Windows.Controls;
using HR_ERP.Data;
using HR_ERP.Helpers;
using HR_ERP.Models;
using Localization = HR_ERP.Helpers.Localization;

namespace HR_ERP.Views
{
    public partial class OrgStructureView : UserControl
    {
        public OrgStructureView()
        {
            InitializeComponent();
            ApplyLocalization();
            LoadStructure();
        }

        private void ApplyLocalization()
        {
            TitleText.Text = Localization.T("OrgStructure.Title");
            RefreshButton.Content = Localization.T("OrgStructure.Refresh");
            ByDepartmentTab.Header = Localization.T("OrgStructure.Tab.ByDepartment");
            ReportingLinesTab.Header = Localization.T("OrgStructure.Tab.ReportingLines");
            VacantRolesTab.Header = Localization.T("OrgStructure.Tab.VacantRoles");
            ReportingNoteText.Text = Localization.T("OrgStructure.ReportingNote");
            VacantNoteText.Text = Localization.T("OrgStructure.VacantNote");
            ColDepartment.Header = Localization.T("OrgStructure.Col.Department");
            ColPosition.Header = Localization.T("OrgStructure.Col.Position");
            ColPositionCode.Header = Localization.T("OrgStructure.Col.PositionCode");
            NoVacanciesText.Text = Localization.T("OrgStructure.NoVacancies");
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadStructure();

        private void LoadStructure()
        {
            try
            {
                var departments = DepartmentRepository.GetAll();
                var employees = EmployeeRepository.GetAll().Where(e => e.EmploymentStatus == "Active").ToList();

                LoadDepartmentCards(departments, employees);
                LoadReportingLines(employees);
                LoadVacantRoles(departments, employees);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load organizational structure: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDepartmentCards(List<Department> departments, List<Employee> employees)
        {
            var mainDepartments = departments.Where(d => string.IsNullOrWhiteSpace(d.ParentCode)).ToList();

            var cards = mainDepartments.Select(dept =>
            {
                var subDepts = departments.Where(d => string.Equals(d.ParentCode, dept.Code, StringComparison.OrdinalIgnoreCase)).ToList();

                // Employees counted at the main department belong there directly (not via a sub-department).
                var directEmployees = employees
                    .Where(e => string.Equals(e.Depart, dept.Name, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(e => e.Name)
                    .ToList();

                int totalCount = directEmployees.Count;

                var subCards = subDepts.Select(sub =>
                {
                    var subEmployees = employees
                        .Where(e => string.Equals(e.Depart, sub.Name, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(e => e.Name)
                        .ToList();
                    totalCount += subEmployees.Count;

                    var subManager = FindManager(sub, employees);

                    return BuildCard(sub.Name, subEmployees, subManager, isSub: true);
                }).ToList();

                var manager = FindManager(dept, employees);
                var card = BuildCard(dept.Name, directEmployees, manager, isSub: false);
                card.EmployeeCountText = string.Format(Localization.T("OrgStructure.EmployeeCountTotal"), totalCount)
                    + (subCards.Count > 0 ? string.Format(Localization.T("OrgStructure.AcrossSubDepts"), subCards.Count) : "");
                card.SubDepartments = subCards;
                return card;
            }).ToList();

            // Employees whose `depart` doesn't match any department (main or sub) at all — surfaced,
            // not silently dropped, so data-entry mismatches are visible.
            var allDeptNames = departments.Select(d => d.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var unassigned = employees.Where(e => !allDeptNames.Contains(e.Depart ?? "")).OrderBy(e => e.Name).ToList();

            if (unassigned.Count > 0)
                cards.Add(BuildCard(Localization.T("OrgStructure.NoDepartment"), unassigned, null, isSub: false));

            DepartmentsPanel.ItemsSource = cards;
        }

        /// <summary>Builds the manager → direct-reports tree from employee.manager_code. Employees
        /// with no manager (or whose manager isn't an active employee) become top-level nodes.
        /// Guards against a manager cycle by tracking visited codes during the recursive build.</summary>
        private void LoadReportingLines(List<Employee> employees)
        {
            var byCode = employees.ToDictionary(e => e.Code, e => e, StringComparer.OrdinalIgnoreCase);

            var topLevel = employees.Where(e =>
                string.IsNullOrWhiteSpace(e.ManagerCode) || !byCode.ContainsKey(e.ManagerCode!));

            var nodes = topLevel
                .OrderBy(e => e.Name)
                .Select(e => BuildReportNode(e, employees, new HashSet<string>(StringComparer.OrdinalIgnoreCase)))
                .ToList();

            ReportingTree.ItemsSource = nodes;
        }

        private static ReportNode BuildReportNode(Employee employee, List<Employee> allEmployees, HashSet<string> visited)
        {
            visited.Add(employee.Code);

            var reports = allEmployees
                .Where(e => string.Equals(e.ManagerCode, employee.Code, StringComparison.OrdinalIgnoreCase) && !visited.Contains(e.Code))
                .OrderBy(e => e.Name)
                .Select(e => BuildReportNode(e, allEmployees, visited))
                .ToList();

            return new ReportNode
            {
                Name = employee.Name,
                Title = employee.Position ?? "",
                CountText = reports.Count > 0 ? string.Format(Localization.T("OrgStructure.DirectReports"), reports.Count) : "",
                DirectReports = reports
            };
        }

        /// <summary>A defined JobPosition with no active employee currently holding it (same
        /// department + matching title, case-insensitive) is treated as vacant.</summary>
        private void LoadVacantRoles(List<Department> departments, List<Employee> employees)
        {
            var positions = JobPositionRepository.GetAll();
            var filled = employees
                .Where(e => !string.IsNullOrWhiteSpace(e.Position))
                .Select(e => ((e.Depart ?? "").ToUpperInvariant(), (e.Position ?? "").ToUpperInvariant()))
                .ToHashSet();

            var vacant = positions
                .Where(p => !filled.Contains(((p.Depart ?? "").ToUpperInvariant(), p.Title.ToUpperInvariant())))
                .OrderBy(p => p.Depart).ThenBy(p => p.Title)
                .Select(p => new VacantRole { Department = p.Depart ?? "", PositionTitle = p.Title, PositionCode = p.Code ?? "" })
                .ToList();

            VacantRolesGrid.ItemsSource = vacant;
            NoVacanciesText.Visibility = vacant.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private static Employee? FindManager(Department dept, List<Employee> employees) =>
            string.IsNullOrWhiteSpace(dept.EmplCode)
                ? null
                : employees.FirstOrDefault(e => string.Equals(e.Code, dept.EmplCode, StringComparison.OrdinalIgnoreCase));

        private static DepartmentCard BuildCard(string name, List<Employee> employees, Employee? manager, bool isSub) => new()
        {
            DepartmentName = name,
            EmployeeCountText = isSub ? string.Format(Localization.T("OrgStructure.EmployeeCount"), employees.Count) : "", // main card's count is overwritten by the caller with the aggregate total
            ManagerName = manager != null ? $"{manager.Name} ({manager.Position})" : "",
            ManagerLabelText = Localization.T("OrgStructure.ManagerLabel"),
            ManagerVisibility = manager != null ? Visibility.Visible : Visibility.Collapsed,
            Employees = employees.Select(e => new EmployeeCard { Name = e.Name, Position = e.Position ?? "" }).ToList(),
            EmptyText = isSub ? Localization.T("OrgStructure.NoEmployeesInSubDept") : Localization.T("OrgStructure.NoEmployeesInDept"),
            EmptyVisibility = employees.Count == 0 ? Visibility.Visible : Visibility.Collapsed
        };

        private class DepartmentCard
        {
            public string DepartmentName { get; set; } = "";
            public string EmployeeCountText { get; set; } = "";
            public string ManagerName { get; set; } = "";
            public string ManagerLabelText { get; set; } = "";
            public Visibility ManagerVisibility { get; set; }
            public List<EmployeeCard> Employees { get; set; } = new();
            public string EmptyText { get; set; } = "";
            public Visibility EmptyVisibility { get; set; }
            public List<DepartmentCard> SubDepartments { get; set; } = new();
        }

        private class EmployeeCard
        {
            public string Name { get; set; } = "";
            public string Position { get; set; } = "";
        }

        private class ReportNode
        {
            public string Name { get; set; } = "";
            public string Title { get; set; } = "";
            public string CountText { get; set; } = "";
            public List<ReportNode> DirectReports { get; set; } = new();
        }

        private class VacantRole
        {
            public string Department { get; set; } = "";
            public string PositionTitle { get; set; } = "";
            public string PositionCode { get; set; } = "";
        }
    }
}
