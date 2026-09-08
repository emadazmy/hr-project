using System.Windows;
using System.Windows.Controls;
using HR_ERP.Helpers;
using HR_ERP.Views;
using Localization = HR_ERP.Helpers.Localization;

namespace HR_ERP
{
    public partial class MainWindow : Window
    {
        // Maps each nav button to the module key it's gated by, so permission checks and
        // localization can loop over them instead of repeating the same code per button.
        private (Button Button, string ModuleKey, string LabelKey, Func<UserControl> Factory)[] _navItems = null!;

        public MainWindow()
        {
            InitializeComponent();
            BuildNavItems();

            Localization.LanguageChanged += OnLanguageChanged;
            Closed += (_, _) =>
            {
                Localization.LanguageChanged -= OnLanguageChanged;
                Application.Current.Shutdown();
            };

            ApplyLocalizationAndDirection();
            ApplyPermissions();
            SizeToScreenResolution();

            // Open on the first module the signed-in user can actually see (falls back to
            // Settings if somehow nothing else is granted, since Settings itself is what
            // fixes a misconfigured role).
            var firstAllowed = _navItems.FirstOrDefault(n => Session.CanView(n.ModuleKey));
            MainContent.Content = firstAllowed.Factory != null ? firstAllowed.Factory() : new SettingsView();
        }

        private void BuildNavItems()
        {
            _navItems = new (Button, string, string, Func<UserControl>)[]
            {
                (NavDashboardButton, "Dashboard", "Nav.Dashboard", () => new DashboardView()),
                (NavEmployeesButton, "Employees", "Nav.Employees", () => new EmployeesView()),
                (NavDepartmentsButton, "Departments", "Nav.Departments", () => new DepartmentsView()),
                (NavOrgStructureButton, "OrgStructure", "Nav.OrgStructure", () => new OrgStructureView()),
                (NavShiftsButton, "Shifts", "Nav.Shifts", () => new ShiftsView()),
                (NavAttendanceButton, "Attendance", "Nav.Attendance", () => new AttendanceView()),
                (NavLeaveButton, "Leave", "Nav.Leave", () => new LeaveView()),
                (NavAdjustmentsButton, "Adjustments", "Nav.Adjustments", () => new AdjustmentsView()),
                (NavCashAdvancesButton, "CashAdvances", "Nav.CashAdvances", () => new CashAdvancesView()),
                (NavPayrollButton, "Payroll", "Nav.Payroll", () => new PayrollView()),
                (NavDocumentsButton, "Documents", "Nav.Documents", () => new DocumentsView()),
                (NavSettingsButton, "Settings", "Nav.Settings", () => new SettingsView()),
            };
        }

        private void ApplyLocalizationAndDirection()
        {
            FlowDirection = Localization.FlowDirection;
            Title = Localization.T("AppTitle") + " - " + Localization.T("AppSubtitle");
            AppTitleText.Text = Localization.T("AppTitle");
            AppSubtitleText.Text = Localization.T("AppSubtitle");
            NavSignOutButton.Content = Localization.T("Nav.SignOut");

            foreach (var item in _navItems)
                item.Button.Content = Localization.T(item.LabelKey);

            if (Session.CurrentUser != null)
                CurrentUserText.Text = $"{Session.CurrentUser.FullName} ({Session.CurrentUser.RoleName})";
        }

        /// <summary>Hides nav buttons the signed-in user's role can't view — the actual
        /// access control, not just cosmetic; each view's own repositories still run under
        /// the same shared DB login, so this UI-level gate is what "Permissions" means for
        /// this desktop app (no separate per-request server to enforce it against).</summary>
        private void ApplyPermissions()
        {
            foreach (var item in _navItems)
                item.Button.Visibility = Session.CanView(item.ModuleKey) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnLanguageChanged()
        {
            ApplyLocalizationAndDirection();
        }

        /// <summary>
        /// Auto-detects the current display's working area (SystemParameters, which reflects
        /// whatever screen/resolution the app is actually running on) and sizes the window to
        /// fit it, rather than using a fixed pixel size that might be too big for a small
        /// laptop screen or too small on a large monitor. Every view's layout already uses
        /// Grid star-sizing + ScrollViewers, so they reflow correctly at whatever size this
        /// resolves to — this just picks a sensible starting size/position.
        /// </summary>
        private void SizeToScreenResolution()
        {
            double workWidth = SystemParameters.WorkArea.Width;
            double workHeight = SystemParameters.WorkArea.Height;

            // On a big desktop monitor, a maximized 4K window makes forms/text feel sparse and
            // spread out — cap the restored size to a comfortable proportion of the screen
            // instead of always going edge-to-edge, but still respect small screens exactly.
            Width = Math.Max(MinWidth, Math.Min(workWidth * 0.9, 1600));
            Height = Math.Max(MinHeight, Math.Min(workHeight * 0.9, 950));

            // If even the capped size doesn't comfortably fit (small laptop/tablet screen),
            // just maximize instead.
            if (workWidth < 1000 || workHeight < 650)
                WindowState = WindowState.Maximized;
        }

        private void NavDashboard_Click(object sender, RoutedEventArgs e)
            => MainContent.Content = new DashboardView();

        private void NavEmployees_Click(object sender, RoutedEventArgs e)
            => MainContent.Content = new EmployeesView();

        private void NavDepartments_Click(object sender, RoutedEventArgs e)
            => MainContent.Content = new DepartmentsView();

        private void NavOrgStructure_Click(object sender, RoutedEventArgs e)
            => MainContent.Content = new OrgStructureView();

        private void NavShifts_Click(object sender, RoutedEventArgs e)
            => MainContent.Content = new ShiftsView();

        private void NavAttendance_Click(object sender, RoutedEventArgs e)
            => MainContent.Content = new AttendanceView();

        private void NavLeave_Click(object sender, RoutedEventArgs e)
            => MainContent.Content = new LeaveView();

        private void NavAdjustments_Click(object sender, RoutedEventArgs e)
            => MainContent.Content = new AdjustmentsView();

        private void NavCashAdvances_Click(object sender, RoutedEventArgs e)
            => MainContent.Content = new CashAdvancesView();

        private void NavPayroll_Click(object sender, RoutedEventArgs e)
            => MainContent.Content = new PayrollView();

        private void NavDocuments_Click(object sender, RoutedEventArgs e)
            => MainContent.Content = new DocumentsView();

        private void NavSettings_Click(object sender, RoutedEventArgs e)
            => MainContent.Content = new SettingsView();

        private void NavSignOut_Click(object sender, RoutedEventArgs e)
        {
            Session.SignOut();

            var login = new LoginWindow();
            if (login.ShowDialog() == true && login.AuthenticatedUser != null)
            {
                Session.SignIn(login.AuthenticatedUser);
                ApplyLocalizationAndDirection();
                ApplyPermissions();
                var firstAllowed = _navItems.FirstOrDefault(n => Session.CanView(n.ModuleKey));
                MainContent.Content = firstAllowed.Factory != null ? firstAllowed.Factory() : new SettingsView();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }
    }
}
