using System.Windows;
using System.Windows.Controls;
using Localization = HR_ERP.Helpers.Localization;
using HR_ERP.Data;

namespace HR_ERP.Views
{
    public partial class DashboardView : UserControl
    {
        private const double MaxBarWidth = 220;

        public DashboardView()
        {
            InitializeComponent();
            TodayText.Text = DateTime.Today.ToString("D", System.Globalization.CultureInfo.CurrentCulture);

            // Wire localization changes
            Localization.LanguageChanged += OnLanguageChanged;
            this.Unloaded += (_, _) => Localization.LanguageChanged -= OnLanguageChanged;
            ApplyLocalization();

            LoadDashboard();
        }

        private void OnLanguageChanged() => ApplyLocalization();

        private void ApplyLocalization()
        {
            this.FlowDirection = Localization.FlowDirection;

            HeaderText.Text = Localization.T("Dashboard.Header");
            RefreshButton.Content = Localization.T("Dashboard.Refresh");

            ActiveEmployeesLabel.Text = Localization.T("Dashboard.ActiveEmployees");
            PresentTodayLabel.Text = Localization.T("Dashboard.PresentToday");
            LateTodayLabel.Text = Localization.T("Dashboard.LateToday");
            AbsentTodayLabel.Text = Localization.T("Dashboard.AbsentToday");
            OnLeaveTodayLabel.Text = Localization.T("Dashboard.OnLeaveToday");
            PendingLeaveLabel.Text = Localization.T("Dashboard.PendingLeaveRequests");
            DepartmentsLabel.Text = Localization.T("Dashboard.Departments");
            HeadcountLabel.Text = Localization.T("Dashboard.HeadcountByDepartment");
            UpcomingHolidaysLabel.Text = Localization.T("Dashboard.UpcomingHolidays");
            PendingLeaveHeaderText.Text = Localization.T("Dashboard.PendingLeaveHeader");

            // Month payroll label is set dynamically when data loads, but ensure default localized form
            if (string.IsNullOrWhiteSpace(MonthPayrollLabelText.Text))
                MonthPayrollLabelText.Text = Localization.T("Dashboard.MonthPayroll.Generated");

            NoHolidaysText.Text = Localization.T("Dashboard.NoUpcomingHolidays");
            NoPendingLeaveText.Text = Localization.T("Dashboard.NoPendingLeaveRequests");
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadDashboard();

        private void LoadDashboard()
        {
            try
            {
                var employees = EmployeeRepository.GetAll();
                var activeEmployees = employees.Where(x => x.EmploymentStatus == "Active").ToList();

                var todayAttendance = AttendanceRepository.GetByDate(DateTime.Today);
                int present = todayAttendance.Count(a => string.Equals(a.Status1, "Present", StringComparison.OrdinalIgnoreCase)
                                                       || string.Equals(a.Status1, "Late", StringComparison.OrdinalIgnoreCase));
                int late = todayAttendance.Count(a => string.Equals(a.Status1, "Late", StringComparison.OrdinalIgnoreCase));
                int absent = todayAttendance.Count(a => string.Equals(a.Status1, "Absent", StringComparison.OrdinalIgnoreCase));

                var leaves = LeaveRequestRepository.GetAll();
                int onLeaveToday = leaves.Count(l => string.Equals(l.Status, "Approved", StringComparison.OrdinalIgnoreCase)
                                                    && DateTime.Today >= l.StartDate.Date && DateTime.Today <= l.EndDate.Date);
                var pendingLeaves = leaves.Where(l => string.Equals(l.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                                           .OrderBy(l => l.StartDate)
                                           .Take(6)
                                           .ToList();

                var departments = DepartmentRepository.GetAll();

                var payroll = PayrollRepository.GetAll()
                    .Where(p => p.PayPeriodStart.Year == DateTime.Today.Year && p.PayPeriodStart.Month == DateTime.Today.Month)
                    .ToList();
                decimal monthNetPayroll = payroll.Sum(p => p.NetSalary);

                var upcomingHolidays = HolidayRepository.GetAll()
                    .Where(h => h.HolidayDate.Date >= DateTime.Today)
                    .OrderBy(h => h.HolidayDate)
                    .Take(5)
                    .ToList();

                // ---- KPI cards ----
                TotalEmployeesText.Text = activeEmployees.Count.ToString();
                PresentTodayText.Text = present.ToString();
                LateTodayText.Text = late.ToString();
                AbsentTodayText.Text = absent.ToString();
                OnLeaveTodayText.Text = onLeaveToday.ToString();
                PendingLeaveText.Text = pendingLeaves.Count.ToString();
                DepartmentsText.Text = departments.Count.ToString();
                MonthPayrollText.Text = monthNetPayroll.ToString("N0");
                MonthPayrollLabelText.Text = payroll.Count > 0
                    ? Localization.T("Dashboard.MonthPayroll.Generated")
                    : Localization.T("Dashboard.MonthPayroll.NotGenerated");

                // ---- Department headcount bars ----
                var byDept = activeEmployees
                    .GroupBy(e => string.IsNullOrWhiteSpace(e.Depart) ? "(No Department)" : e.Depart)
                    .Select(g => new { Depart = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .ToList();
                int maxCount = byDept.Count > 0 ? byDept.Max(g => g.Count) : 1;

                DepartmentBarsList.ItemsSource = byDept.Select(g => new DepartmentBarItem
                {
                    Label = $"{g.Depart}  ({g.Count})",
                    BarWidth = maxCount == 0 ? 0 : MaxBarWidth * g.Count / maxCount
                }).ToList();

                // ---- Upcoming holidays ----
                UpcomingHolidaysList.ItemsSource = upcomingHolidays.Select(h => new HolidayDisplayItem
                {
                    DateText = h.HolidayDate.ToString("MMM d, yyyy"),
                    Description = h.Description ?? ""
                }).ToList();
                NoHolidaysText.Visibility = upcomingHolidays.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

                // ---- Pending leave requests ----
                PendingLeaveList.ItemsSource = pendingLeaves.Select(l => new PendingLeaveItem
                {
                    EmplName = l.EmplName ?? l.EmpCode,
                    LeaveType = l.LeaveType ?? "Leave",
                    DateRangeText = $"{l.StartDate:MMM d} – {l.EndDate:MMM d}"
                }).ToList();
                NoPendingLeaveText.Visibility = pendingLeaves.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Localization.T("Dashboard.Error.CouldNotLoad"), ex.Message), Localization.T("Attendance.Error.Title"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private class DepartmentBarItem
        {
            public string Label { get; set; } = "";
            public double BarWidth { get; set; }
        }

        private class HolidayDisplayItem
        {
            public string DateText { get; set; } = "";
            public string Description { get; set; } = "";
        }

        private class PendingLeaveItem
        {
            public string EmplName { get; set; } = "";
            public string LeaveType { get; set; } = "";
            public string DateRangeText { get; set; } = "";
        }
    }
}
