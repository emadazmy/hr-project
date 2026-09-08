using DocumentFormat.OpenXml.Drawing.Diagrams;
using HR_ERP.Data;
using HR_ERP.Helpers;
using HR_ERP.Models;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Localization = HR_ERP.Helpers.Localization;

namespace HR_ERP.Views
{
    public partial class LeaveView : UserControl
    {
        private LeaveRequest? _selected;
        private Holiday? _selectedHoliday;
        private bool _balanceLoaded;

        /// <summary>Once remaining Annual Leave balance (after accounting for this employee's
        /// own other Pending requests) drops to or below this many days, the live indicator and
        /// the post-submit message switch from a plain confirmation to a "running low" warning.
        /// Exhausted (zero or less) always gets its own distinct message regardless of this.</summary>
        private const int LowBalanceWarningDays = 3;

        public LeaveView()
        {
            InitializeComponent();
            ApplyLocalization();
            LoadLookups();
            LoadLeaveRequests();
            LoadHolidays();
        }

        /// <summary>Translates every label, header, button and note on this screen. The
        /// underlying data values it displays — LeaveType (e.g. "Annual Leave") and Status
        /// (e.g. "Approved") — are deliberately left in English regardless of language: they're
        /// stored as literal text and matched against elsewhere (Dashboard counts, payroll
        /// generation, attendance recalculation, the row-color triggers just below), so
        /// translating the displayed text would silently break those comparisons.</summary>
        private void ApplyLocalization()
        {
            TitleText.Text = Localization.T("Leave.Title");
            RequestsTab.Header = Localization.T("Leave.Tab.Requests");
            HolidaysTab.Header = Localization.T("Leave.Tab.Holidays");

            ColCode.Header = Localization.T("Leave.Col.Code");
            ColEmployee.Header = Localization.T("Leave.Col.Employee");
            ColType.Header = Localization.T("Leave.Col.Type");
            ColStart.Header = Localization.T("Leave.Col.Start");
            ColEnd.Header = Localization.T("Leave.Col.End");
            ColReason.Header = Localization.T("Leave.Col.Reason");
            ColStatus.Header = Localization.T("Leave.Col.Status");

            EmployeeLabel.Content = Localization.T("Leave.Field.Employee");
            TypeLabel.Content = Localization.T("Leave.Field.Type");
            StartDateLabel.Content = Localization.T("Leave.Field.StartDate");
            EndDateLabel.Content = Localization.T("Leave.Field.EndDate");
            ReasonLabel.Content = Localization.T("Leave.Field.Reason");
            PaidNoteText.Text = Localization.T("Leave.PaidNote");

            NewButton.Content = Localization.T("Leave.Button.New");
            SubmitButton.Content = Localization.T("Leave.Button.Submit");
            ApproveButton.Content = Localization.T("Leave.Button.Approve");
            RejectButton.Content = Localization.T("Leave.Button.Reject");
            DeleteButton.Content = Localization.T("Leave.Button.Delete");

            BalanceExpander.Header = Localization.T("Leave.Balance.Header");
            BalanceNoteText.Text = Localization.T("Leave.Balance.Note");
            BalanceRefreshButton.Content = Localization.T("Leave.Balance.Refresh");

            ColBalCode.Header = Localization.T("Leave.Balance.Col.Code");
            ColBalEmployee.Header = Localization.T("Leave.Balance.Col.Employee");
            ColBalEntitlement.Header = Localization.T("Leave.Balance.Col.Entitlement");
            ColBalCarriedOver.Header = Localization.T("Leave.Balance.Col.CarriedOver");
            ColBalTotalAvailable.Header = Localization.T("Leave.Balance.Col.TotalAvailable");
            ColBalUsedThisYear.Header = Localization.T("Leave.Balance.Col.UsedThisYear");
            ColBalRemaining.Header = Localization.T("Leave.Balance.Col.Remaining");

            ColHolidayDate.Header = Localization.T("Leave.Holidays.Col.Date");
            ColHolidayDescription.Header = Localization.T("Leave.Holidays.Col.Description");
            HolidayDateLabel.Content = Localization.T("Leave.Holidays.Field.Date");
            HolidayDescriptionLabel.Content = Localization.T("Leave.Holidays.Field.Description");
            HolidayNewButton.Content = Localization.T("Leave.Holidays.Button.New");
            HolidaySaveButton.Content = Localization.T("Leave.Holidays.Button.Save");
            HolidayDeleteButton.Content = Localization.T("Leave.Holidays.Button.Delete");

            if (LeaveTypeBox != null)
            {
                var items = LeaveTypeBox.Items.OfType<ComboBoxItem>().ToList();
                if (items.Count >= 6)
                {
                    items[0].Content = Localization.T("Leave.Type.Annual"); items[0].Tag = "Annual Leave";
                    items[1].Content = Localization.T("Leave.Type.Sick"); items[1].Tag = "Sick Leave";
                    items[2].Content = Localization.T("Leave.Type.Unpaid"); items[2].Tag = "Unpaid Leave";
                    items[3].Content = Localization.T("Leave.Type.Maternity"); items[3].Tag = "Maternity/Paternity Leave";
                    items[4].Content = Localization.T("Leave.Type.Mission"); items[4].Tag = "Official Mission";
                    items[5].Content = Localization.T("Leave.Type.Permit"); items[5].Tag = "Departure Permit";
                }
            }
        }

        // ---------------- Leave / Mission / Permit requests ----------------

        private void LoadLookups()
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

        private void LoadLeaveRequests() => LeaveGrid.ItemsSource = LeaveRequestRepository.GetAll();

        private void LeaveGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LeaveGrid.SelectedItem is not LeaveRequest lr) return;
            _selected = lr;

            EmployeeBox.SelectedItem = (EmployeeBox.ItemsSource as List<Employee>)?.FirstOrDefault(x => x.Code == lr.EmpCode);
            foreach (ComboBoxItem item in LeaveTypeBox.Items)
                if (string.Equals(item.Content?.ToString(), lr.LeaveType, StringComparison.OrdinalIgnoreCase))
                    LeaveTypeBox.SelectedItem = item;
            StartDatePicker.SelectedDate = lr.StartDate;
            EndDatePicker.SelectedDate = lr.EndDate;
            ReasonBox.Text = lr.Reason;
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _selected = null;
            EmployeeBox.SelectedItem = null;
            LeaveTypeBox.SelectedItem = null;
            StartDatePicker.SelectedDate = EndDatePicker.SelectedDate = DateTime.Today;
            ReasonBox.Text = "";
            LeaveGrid.SelectedItem = null;
        }

        /// <summary>Recomputes and shows the live Annual Leave balance indicator whenever the
        /// employee, leave type, or either date changes — the same effective-remaining figure
        /// (approved balance minus this employee's other currently-Pending Annual Leave days)
        /// that Submit_Click enforces, so what the person sees here never contradicts what
        /// happens when they click Submit.</summary>
        private void BalanceCheckTrigger_Changed(object sender, SelectionChangedEventArgs e) => UpdateRemainingBalanceDisplay();

        private void UpdateRemainingBalanceDisplay()
        {
            if (EmployeeBox.SelectedItem is not Employee emp || LeaveTypeBox.SelectedItem is not ComboBoxItem lt
                || !string.Equals(lt.Tag?.ToString(), "Annual Leave", StringComparison.OrdinalIgnoreCase))
            {
                RemainingBalanceText.Visibility = Visibility.Collapsed;
                return;
            }

            int effectiveRemaining = GetEffectiveRemainingBalance(emp);
            int requestedDays = 0;
            if (StartDatePicker.SelectedDate != null && EndDatePicker.SelectedDate != null
                && EndDatePicker.SelectedDate >= StartDatePicker.SelectedDate)
                requestedDays = (EndDatePicker.SelectedDate.Value - StartDatePicker.SelectedDate.Value).Days + 1;

            RemainingBalanceText.Visibility = Visibility.Visible;

            if (requestedDays > 0 && requestedDays > effectiveRemaining)
            {
                RemainingBalanceText.Text = string.Format(Localization.T("Leave.Balance.WouldExceed"), effectiveRemaining, requestedDays);
                RemainingBalanceText.Foreground = System.Windows.Media.Brushes.Red;
            }
            else if (effectiveRemaining <= 0)
            {
                RemainingBalanceText.Text = Localization.T("Leave.Balance.AlreadyExhausted");
                RemainingBalanceText.Foreground = System.Windows.Media.Brushes.Red;
            }
            else if (effectiveRemaining <= LowBalanceWarningDays)
            {
                RemainingBalanceText.Text = string.Format(Localization.T("Leave.Balance.Low"), effectiveRemaining);
                RemainingBalanceText.Foreground = System.Windows.Media.Brushes.DarkOrange;
            }
            else
            {
                RemainingBalanceText.Text = string.Format(Localization.T("Leave.Balance.OK"), effectiveRemaining);
                RemainingBalanceText.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        /// <summary>Approved-based remaining balance minus this employee's own other
        /// currently-Pending Annual Leave requests for the current year — the figure actually
        /// enforced, so several pending requests can't collectively slip past the entitlement.</summary>
        private static int GetEffectiveRemainingBalance(Employee emp)
        {
            var balance = LeaveRequestRepository.GetBalanceForEmployee(emp);
            int pendingReserved = LeaveRequestRepository.GetPendingAnnualLeaveDaysReserved(emp.Code, DateTime.Today.Year);
            return balance.RemainingBalance - pendingReserved;
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeeBox.SelectedItem is not Employee emp || LeaveTypeBox.SelectedItem is not ComboBoxItem lt
                || StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Employee, type, start and end dates are required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EndDatePicker.SelectedDate < StartDatePicker.SelectedDate)
            {
                MessageBox.Show("End date cannot be before start date.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Annual Leave specifically is capped at the employee's remaining balance — every
            // other leave type here (Sick, Unpaid, Maternity/Paternity, Official Mission,
            // Departure Permit) has no such entitlement to check against.
            bool isAnnualLeave = string.Equals(lt.Tag?.ToString(), "Annual Leave", StringComparison.OrdinalIgnoreCase);
            int requestedDays = (EndDatePicker.SelectedDate!.Value - StartDatePicker.SelectedDate!.Value).Days + 1;
            int effectiveRemainingBeforeThis = 0;

            if (isAnnualLeave)
            {
                effectiveRemainingBeforeThis = GetEffectiveRemainingBalance(emp);
                if (requestedDays > effectiveRemainingBeforeThis)
                {
                    MessageBox.Show(
                        string.Format(Localization.T("Leave.Balance.BlockMessage"), effectiveRemainingBeforeThis, requestedDays),
                        Localization.T("Common.Validation"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var request = new LeaveRequest
            {
                EmpCode = emp.Code,
                EmplName = emp.Name,
                LeaveType = lt.Tag?.ToString(),
                StartDate = StartDatePicker.SelectedDate.Value,
                EndDate = EndDatePicker.SelectedDate.Value,
                Reason = ReasonBox.Text.Trim(),
                Status = "Pending"
            };

            try
            {
                LeaveRequestRepository.Add(request);
                LoadLeaveRequests();

                if (isAnnualLeave)
                {
                    int effectiveRemainingAfterThis = effectiveRemainingBeforeThis - requestedDays;
                    if (effectiveRemainingAfterThis <= 0)
                        MessageBox.Show(Localization.T("Leave.Balance.AlreadyExhausted"), Localization.T("Common.Info"), MessageBoxButton.OK, MessageBoxImage.Information);
                    else if (effectiveRemainingAfterThis <= LowBalanceWarningDays)
                        MessageBox.Show(string.Format(Localization.T("Leave.Balance.Low"), effectiveRemainingAfterThis), Localization.T("Common.Info"), MessageBoxButton.OK, MessageBoxImage.Information);
                }

                New_Click(sender, e);
                if (_balanceLoaded) LoadBalances();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Submit failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Approve_Click(object sender, RoutedEventArgs e) => ChangeStatus("Approved");
        private void Reject_Click(object sender, RoutedEventArgs e) => ChangeStatus("Rejected");

        private void ChangeStatus(string status)
        {
            if (_selected == null)
            {
                MessageBox.Show("Select a leave request first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var affectedEmpCode = _selected.EmpCode;
                var affectedStart = _selected.StartDate;
                var affectedEnd = _selected.EndDate;

                LeaveRequestRepository.UpdateStatus(_selected.LeaveID, status);
                LoadLeaveRequests();
                New_Click(this, new RoutedEventArgs());
                if (_balanceLoaded) LoadBalances();

                // Any existing Attendance rows in this date range need their status
                // recalculated now that the leave's approval status changed (skips rows
                // already marked Approved — those are left alone for manual review).
                AttendanceRecalculationService.RecalculateEmployeeRange(affectedEmpCode, affectedStart, affectedEnd);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Update failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_selected == null) return;
            if (MessageBox.Show("Delete this request?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            try
            {
                bool wasApproved = string.Equals(_selected.Status, "Approved", StringComparison.OrdinalIgnoreCase);
                var affectedEmpCode = _selected.EmpCode;
                var affectedStart = _selected.StartDate;
                var affectedEnd = _selected.EndDate;

                LeaveRequestRepository.Delete(_selected.LeaveID);
                LoadLeaveRequests();
                New_Click(sender, e);

                // If the deleted request was approved, any Attendance rows in its range were
                // showing "Leave" because of it — recalculate them back to normal.
                if (wasApproved)
                    AttendanceRecalculationService.RecalculateEmployeeRange(affectedEmpCode, affectedStart, affectedEnd);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Delete failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------------- Leave balance summary ----------------

        private void BalanceExpander_Expanded(object sender, RoutedEventArgs e)
        {
            if (_balanceLoaded) return; // lazy-load once; use the Refresh button afterward
            LoadBalances();
        }

        private void BalanceRefresh_Click(object sender, RoutedEventArgs e) => LoadBalances();

        private void LoadBalances()
        {
            try
            {
                BalanceGrid.ItemsSource = LeaveRequestRepository.GetLeaveBalances();
                _balanceLoaded = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load leave balances: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------------- Company Holidays tab ----------------

        private void LoadHolidays()
        {
            try { HolidaysGrid.ItemsSource = HolidayRepository.GetAll(); }
            catch (Exception ex) { MessageBox.Show("Could not load holidays: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void HolidaysGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HolidaysGrid.SelectedItem is not Holiday h) return;
            _selectedHoliday = h;
            HolidayDatePicker.SelectedDate = h.HolidayDate;
            HolidayDescriptionBox.Text = h.Description;
        }

        private void HolidayNew_Click(object sender, RoutedEventArgs e)
        {
            _selectedHoliday = null;
            HolidayDatePicker.SelectedDate = DateTime.Today;
            HolidayDescriptionBox.Text = "";
            HolidaysGrid.SelectedItem = null;
        }

        private void HolidaySave_Click(object sender, RoutedEventArgs e)
        {
            if (HolidayDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Date is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var holiday = new Holiday
            {
                HolidayID = _selectedHoliday?.HolidayID ?? 0,
                HolidayDate = HolidayDatePicker.SelectedDate.Value,
                Description = HolidayDescriptionBox.Text.Trim()
            };

            try
            {
                if (_selectedHoliday == null) HolidayRepository.Add(holiday);
                else HolidayRepository.Update(holiday);
                LoadHolidays();

                // A holiday affects everyone — recalculate any existing Attendance rows on
                // this date for every employee (skips rows already marked Approved).
                AttendanceRecalculationService.RecalculateAllEmployeesForDate(holiday.HolidayDate);

                HolidayNew_Click(sender, e);
            }
            catch (Exception ex) { MessageBox.Show("Save failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void HolidayDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedHoliday == null) return;
            if (MessageBox.Show("Delete this holiday?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            try
            {
                var affectedDate = _selectedHoliday.HolidayDate;
                HolidayRepository.Delete(_selectedHoliday.HolidayID);
                LoadHolidays();
                HolidayNew_Click(sender, e);

                // Any Attendance rows that day were showing "Holiday" because of this — recalculate them.
                AttendanceRecalculationService.RecalculateAllEmployeesForDate(affectedDate);
            }
            catch (Exception ex) { MessageBox.Show("Delete failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }
}
