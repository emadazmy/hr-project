using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Localization = HR_ERP.Helpers.Localization;
using HR_ERP.Data;
using HR_ERP.Models;
using Microsoft.Win32;

namespace HR_ERP.Views
{
    public partial class AttendanceView : UserControl
    {
        private AttendanceRecord? _selected;

        public AttendanceView()
        {
            InitializeComponent();
            DatePickerFilter.SelectedDate = DateTime.Today;
            CheckinDatePicker.SelectedDate = DateTime.Today;
            SetThisMonthRange();

            // Wire language change events and apply initial localization
            Localization.LanguageChanged += OnLanguageChanged;
            this.Unloaded += (_, _) => Localization.LanguageChanged -= OnLanguageChanged;
            ApplyLocalization();

            LoadEmployees();
            LoadShifts();
            LoadAttendance();
        }

        private void OnLanguageChanged() => ApplyLocalization();

        // Localization: Apply localized strings to controls from code-behind (restores previous behavior).
        private void ApplyLocalization()
        {
            // Flow direction (MainWindow also flips, but ensure control-level direction follows)
            this.FlowDirection = Localization.FlowDirection;

            // Top bar
            HeaderText.Text = Localization.T("Attendance.Header");
            ImportButton.Content = Localization.T("Attendance.ImportExcel");
            PrintReportButton.Content = Localization.T("Attendance.Report.Button");
            DownloadTemplateButton.Content = Localization.T("Attendance.DownloadTemplate");

            // Mode selectors and filters
            ModeByDayRadio.Content = Localization.T("Attendance.Mode.ByDay");
            ModeByEmployeeRadio.Content = Localization.T("Attendance.Mode.ByEmployee");
            DateLabel.Content = Localization.T("Attendance.Filter.Date");
            EmployeeLabel.Content = Localization.T("Attendance.Filter.Employee");
            FromLabel.Content = Localization.T("Attendance.Filter.From");
            ToLabel.Content = Localization.T("Attendance.Filter.To");
            ThisMonthButton.Content = Localization.T("Attendance.Button.ThisMonth");

            // DataGrid column headers (order must match XAML)
            if (AttendanceGrid?.Columns != null && AttendanceGrid.Columns.Count >= 13)
            {
                AttendanceGrid.Columns[0].Header = Localization.T("Attendance.Column.Code");
                AttendanceGrid.Columns[1].Header = Localization.T("Attendance.Column.Employee");
                AttendanceGrid.Columns[2].Header = Localization.T("Attendance.Column.Date");
                AttendanceGrid.Columns[3].Header = Localization.T("Attendance.Column.Day");
                AttendanceGrid.Columns[4].Header = Localization.T("Attendance.Column.Checkin");
                AttendanceGrid.Columns[5].Header = Localization.T("Attendance.Column.Checkout");
                AttendanceGrid.Columns[6].Header = Localization.T("Attendance.Column.Shift");
                AttendanceGrid.Columns[7].Header = Localization.T("Attendance.Column.Late");
                AttendanceGrid.Columns[8].Header = Localization.T("Attendance.Column.Early");
                AttendanceGrid.Columns[9].Header = Localization.T("Attendance.Column.Overtime");
                AttendanceGrid.Columns[10].Header = Localization.T("Attendance.Column.TotalHrs");
                AttendanceGrid.Columns[11].Header = Localization.T("Attendance.Column.Status");
                AttendanceGrid.Columns[12].Header = Localization.T("Attendance.Column.Approved");
            }

            // Edit pane
            EditExpander.Header = Localization.T("Attendance.Edit.Header");
            EditEmployeeLabel.Content = Localization.T("Attendance.Edit.Employee");
            EditCheckinDateLabel.Content = Localization.T("Attendance.Edit.CheckinDate");
            EditCheckinTimeLabel.Content = Localization.T("Attendance.Edit.CheckinTime");
            EditCheckoutTimeLabel.Content = Localization.T("Attendance.Edit.CheckoutTime");
            EditShiftLabel.Content = Localization.T("Attendance.Edit.Shift");
            ForceRecalculateButton.Content = Localization.T("Attendance.Button.ForceRecalculate");
            StatusLabel.Content = Localization.T("Attendance.Label.Status");

            // Status combo items
            if (StatusBox != null)
            {
                var items = StatusBox.Items.OfType<ComboBoxItem>().ToList();
                if (items.Count >= 6)
                {
                    items[0].Content = Localization.T("Attendance.Status.Present"); items[0].Tag = "Present";
                    items[1].Content = Localization.T("Attendance.Status.Absent"); items[1].Tag = "Absent";
                    items[2].Content = Localization.T("Attendance.Status.Late"); items[2].Tag = "Late";
                    items[3].Content = Localization.T("Attendance.Status.Leave"); items[3].Tag = "Leave";
                    items[4].Content = Localization.T("Attendance.Status.Holiday"); items[4].Tag = "Holiday";
                    items[5].Content = Localization.T("Attendance.Status.Weekend"); items[5].Tag = "Weekend";
                }
            }

            MinutesLateLabel.Content = Localization.T("Attendance.Label.MinutesLate");
            MinutesEarlyLeaveLabel.Content = Localization.T("Attendance.Label.MinutesEarlyLeave");
            WorkingHoursLabel.Content = Localization.T("Attendance.Label.WorkingHours");
            OvertimeLabel.Content = Localization.T("Attendance.Label.Overtime");
            ApprovedCheck.Content = Localization.T("Attendance.Approved");

            NewButton.Content = Localization.T("Attendance.Button.New");
            SaveButton.Content = Localization.T("Attendance.Button.SaveManual");
            DeleteButton.Content = Localization.T("Attendance.Button.Delete");

            // Summary default
            if (string.IsNullOrWhiteSpace(SummaryText.Text))
                SummaryText.Text = Localization.T("Attendance.Summary.PickEmployee");

            // Autosave/help text default
            if (string.IsNullOrWhiteSpace(AutoSaveStatusText.Text))
            {
                AutoSaveStatusText.Text = Localization.T("Attendance.AutoSave.Info");
                AutoSaveStatusText.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void SetThisMonthRange()
        {
            var today = DateTime.Today;
            RangeStartPicker.SelectedDate = new DateTime(today.Year, today.Month, 1);
            RangeEndPicker.SelectedDate = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
        }

        private void LoadShifts()
        {
            try
            {
                ShiftBox.ItemsSource = ShiftRepository.GetAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Localization.T("Attendance.Error.CouldNotLoadShifts"), ex.Message), Localization.T("Attendance.Error.Title"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadEmployees()
        {
            try
            {
                var employees = EmployeeRepository.GetAll();
                EmployeeBox.ItemsSource = employees;
                PreviewEmployeeBox.ItemsSource = employees;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Localization.T("Attendance.Error.CouldNotLoadEmployees"), ex.Message), Localization.T("Attendance.Error.Title"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAttendance()
        {
            // Guard against firing during InitializeComponent before controls exist.
            if (ModeByEmployeeRadio == null) return;

            List<AttendanceRecord> records;

            if (ModeByEmployeeRadio.IsChecked == true)
            {
                if (PreviewEmployeeBox.SelectedItem is not Employee emp
                    || RangeStartPicker.SelectedDate == null || RangeEndPicker.SelectedDate == null)
                {
                    AttendanceGrid.ItemsSource = null;
                    SummaryText.Text = Localization.T("Attendance.Summary.PickEmployee");
                    return;
                }

                records = AttendanceRepository.GetByEmployeeAndDateRange(
                    emp.Code, RangeStartPicker.SelectedDate.Value, RangeEndPicker.SelectedDate.Value);
            }
            else
            {
                var date = DatePickerFilter.SelectedDate ?? DateTime.Today;
                records = AttendanceRepository.GetByDate(date);
            }

            AttendanceGrid.ItemsSource = records;
            UpdateSummary(records);
        }

        private void UpdateSummary(List<AttendanceRecord> records)
        {
            int present = records.Count(r => string.Equals(r.Status1, "Present", StringComparison.OrdinalIgnoreCase));
            int late = records.Count(r => string.Equals(r.Status1, "Late", StringComparison.OrdinalIgnoreCase));
            int absent = records.Count(r => string.Equals(r.Status1, "Absent", StringComparison.OrdinalIgnoreCase));
            int onLeave = records.Count(r => string.Equals(r.Status1, "Leave", StringComparison.OrdinalIgnoreCase));
            decimal totalOvertime = records.Sum(r => r.Overtime);

            // Use localized format
            SummaryText.Text = string.Format(Localization.T("Attendance.Summary.Format"),
                records.Count, present, late, absent, onLeave, totalOvertime);
        }

        private void DatePickerFilter_SelectedDateChanged(object sender, SelectionChangedEventArgs e) => LoadAttendance();

        private void PreviewFilter_Changed(object sender, SelectionChangedEventArgs e) => LoadAttendance();

        private void PreviewMode_Changed(object sender, RoutedEventArgs e)
        {
            // Guard against firing while XAML is still constructing the controls.
            if (ByDayPanel == null || ByEmployeePanel == null) return;

            bool byEmployee = ModeByEmployeeRadio.IsChecked == true;
            ByDayPanel.Visibility = byEmployee ? Visibility.Collapsed : Visibility.Visible;
            ByEmployeePanel.Visibility = byEmployee ? Visibility.Visible : Visibility.Collapsed;
            LoadAttendance();
        }

        private void ThisMonth_Click(object sender, RoutedEventArgs e)
        {
            SetThisMonthRange();
            LoadAttendance();
        }

        private void AttendanceGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AttendanceGrid.SelectedItem is not AttendanceRecord a) return;
            _selected = a;
            EditExpander.IsExpanded = true;
            LoadFormFromRecord(a);
        }

        /// <summary>Populates the form fields from an existing record without running any
        /// calculation — used when selecting a grid row, and when an Approved record is
        /// loaded for review (its computed fields must not be silently recalculated).</summary>
        private void LoadFormFromRecord(AttendanceRecord a)
        {
            EmployeeBox.SelectedItem = (EmployeeBox.ItemsSource as List<Employee>)?.FirstOrDefault(x => x.Code == a.EmployeeCode);
            CheckinDatePicker.SelectedDate = a.CheckinDate;
            CheckinBox.Text = a.Checkin?.ToString(@"hh\:mm") ?? "";
            CheckoutBox.Text = a.Checkout?.ToString(@"hh\:mm") ?? "";
            ShiftBox.SelectedItem = (ShiftBox.ItemsSource as List<Shift>)?.FirstOrDefault(s => s.ShiftID == a.ShiftId);
            SetComboText(StatusBox, a.Status1);
            MinutesLateBox.Text = a.MinutesLate.ToString();
            MinutesEarlyLeaveBox.Text = a.MinutesEarlyLeave.ToString();
            WorkingHoursBox.Text = a.WorkingHours?.ToString() ?? "";
            OvertimeBox.Text = a.Overtime.ToString();
            ApprovedCheck.IsChecked = a.Approved;
        }

        private static void SetComboText(ComboBox box, string? value)
        {
            foreach (ComboBoxItem item in box.Items)
            {
                // Prefer matching the underlying Tag (internal value) if present, otherwise fall back to Content
                var tag = item.Tag?.ToString();
                if (!string.IsNullOrEmpty(tag) && string.Equals(tag, value, StringComparison.OrdinalIgnoreCase))
                {
                    box.SelectedItem = item;
                    return;
                }

                if (string.Equals(item.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase))
                {
                    box.SelectedItem = item;
                    return;
                }
            }
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _selected = null;
            EditExpander.IsExpanded = true;
            EmployeeBox.SelectedItem = null;
            CheckinDatePicker.SelectedDate = DateTime.Today;
            CheckinBox.Text = CheckoutBox.Text = "";
            ShiftBox.SelectedItem = null;
            StatusBox.SelectedIndex = 0;
            MinutesLateBox.Text = MinutesEarlyLeaveBox.Text = OvertimeBox.Text = "0";
            WorkingHoursBox.Text = "";
            ApprovedCheck.IsChecked = false;
            AttendanceGrid.SelectedItem = null;
            AutoSaveStatusText.Text = Localization.T("Attendance.AutoSave.Info");
            AutoSaveStatusText.Foreground = System.Windows.Media.Brushes.Gray;
        }

        private void ShiftBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => RunCalculation();

        private void EmployeeBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => RunCalculation();

        private void CheckinDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e) => RunCalculation();

        private void CheckinOrCheckout_LostFocus(object sender, RoutedEventArgs e)
        {
            if (ReferenceEquals(sender, CheckinBox))
            {
                // Auto-detect the shift from the check-in time (overwrites the current
                // selection — the user can still pick a different shift manually afterward).
                var checkin = ParseTime(CheckinBox.Text);
                var detected = ShiftDetector.Detect(checkin, ShiftBox.ItemsSource as List<Shift> ?? new List<Shift>());
                if (detected != null)
                    ShiftBox.SelectedItem = detected;
            }
            RunCalculation();
        }

        private void Recalculate_Click(object sender, RoutedEventArgs e) => RunCalculation();

        /// <summary>
        /// Runs Layer 1 (AttendanceCalculator) against whatever is currently selected/typed
        /// into Employee/Check-in Date/Check-in/Check-out/Shift, and — unlike earlier versions
        /// of this screen — saves the result immediately. No "Recalculate" or "Save" click is
        /// required for the normal flow; both buttons still exist as an explicit manual
        /// override (see Save_Click).
        ///
        /// If an Attendance row already exists for this employee/date and is marked Approved,
        /// automatic recalculation is skipped entirely — the existing values are loaded for
        /// review, but never silently overwritten. The person can still edit the fields by hand
        /// and click "Save (manual override)", which persists their changes regardless of the
        /// Approved flag; that's a deliberate action, not an automatic one.
        /// </summary>
        private void RunCalculation()
        {
            if (EmployeeBox.SelectedItem is not Employee emp || CheckinDatePicker.SelectedDate == null)
                return; // not enough information yet to calculate or save anything

            var date = CheckinDatePicker.SelectedDate.Value;
            var existing = AttendanceRepository.GetByEmployeeAndDate(emp.Code, date);

            if (existing != null && existing.Approved)
            {
                _selected = existing;
                LoadFormFromRecord(existing);
                AutoSaveStatusText.Text = Localization.T("Attendance.AutoSave.Locked");
                AutoSaveStatusText.Foreground = System.Windows.Media.Brushes.DarkOrange;
                return;
            }

            var checkin = ParseTime(CheckinBox.Text);
            var checkout = ParseTime(CheckoutBox.Text);
            var shift = ShiftBox.SelectedItem as Shift;

            bool isHoliday = HolidayRepository.IsHoliday(date);
            bool isOnLeave = LeaveRequestRepository.IsOnApprovedLeave(emp.Code, date);
            bool isWeekend = AttendanceCalculator.IsWeekend(emp, date);

            var result = AttendanceCalculator.Calculate(checkin, checkout, shift, isHoliday, isOnLeave, isWeekend);

            SetComboText(StatusBox, result.Status);
            MinutesLateBox.Text = result.MinutesLate.ToString();
            MinutesEarlyLeaveBox.Text = result.MinutesEarlyLeave.ToString();
            WorkingHoursBox.Text = result.WorkingHours?.ToString() ?? "";
            OvertimeBox.Text = result.Overtime.ToString();

            // Only persist automatically when there's something real to save: an actual
            // punch was typed, this date is a Weekend/Holiday/Leave (worth recording even
            // without a punch), or we're updating a record that already exists. Just
            // browsing the Employee dropdown with a date picked and nothing else entered
            // should never silently create a new "Absent" row for every employee you pass
            // through — absences are already inferred at the monthly summary level from the
            // lack of an Attendance row, so no row is needed to represent a plain absence.
            bool worthSaving = existing != null || checkin.HasValue || checkout.HasValue || isHoliday || isOnLeave || isWeekend;
            if (worthSaving)
                AutoSave(emp, date);
            else
                AutoSaveStatusText.Text = Localization.T("Attendance.AutoSave.NothingSaved");
        }

        /// <summary>Builds an AttendanceRecord from the form's current values and upserts it —
        /// shared by the automatic flow (RunCalculation) and the explicit manual Save button.</summary>
        private AttendanceRecord BuildRecordFromForm(Employee emp, DateTime date) => new()
        {
            Id = _selected?.Id ?? 0,
            EmployeeCode = emp.Code,
            Depart = emp.Depart,
            Name = emp.Name,
            CheckinDate = date,
            Checkin = ParseTime(CheckinBox.Text),
            CheckoutDate = date,
            Checkout = ParseTime(CheckoutBox.Text),
            ShiftId = (ShiftBox.SelectedItem as Shift)?.ShiftID,
            Status1 = (StatusBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Present",
            MinutesLate = int.TryParse(MinutesLateBox.Text, out var late) ? late : 0,
            MinutesEarlyLeave = int.TryParse(MinutesEarlyLeaveBox.Text, out var early) ? early : 0,
            WorkingHours = int.TryParse(WorkingHoursBox.Text, out var hours) ? hours : null,
            Overtime = decimal.TryParse(OvertimeBox.Text, out var ot) ? ot : 0,
            Approved = ApprovedCheck.IsChecked ?? false,
            DayName = date.ToString("dddd", CultureInfo.InvariantCulture)
        };

        private void AutoSave(Employee emp, DateTime date)
        {
            try
            {
                AttendanceRepository.Upsert(BuildRecordFromForm(emp, date));
                _selected = AttendanceRepository.GetByEmployeeAndDate(emp.Code, date);
                LoadAttendance(); // refresh the grid; the form itself is left as-is so typing can continue

                AutoSaveStatusText.Text = $"✓ Saved automatically at {DateTime.Now:T}.";
                AutoSaveStatusText.Foreground = System.Windows.Media.Brushes.Gray;
            }
            catch (Exception ex)
            {
                AutoSaveStatusText.Text = "⚠ Auto-save failed: " + ex.Message;
                AutoSaveStatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private static TimeSpan? ParseTime(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            return TimeSpan.TryParseExact(text.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out var t) ? t : null;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeeBox.SelectedItem is not Employee emp || CheckinDatePicker.SelectedDate == null)
            {
                MessageBox.Show(Localization.T("Attendance.Save.Validation"), Localization.T("Common.Validation"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var date = CheckinDatePicker.SelectedDate.Value;

            try
            {
                // Explicit save always applies, even over a record marked Approved — this is a
                // deliberate manual override, unlike the automatic RunCalculation flow which
                // refuses to touch Approved records on its own.
                AttendanceRepository.Upsert(BuildRecordFromForm(emp, date));
                _selected = AttendanceRepository.GetByEmployeeAndDate(emp.Code, date);
                LoadAttendance();

                AutoSaveStatusText.Text = string.Format(Localization.T("Attendance.Save.Success"), DateTime.Now.ToString("T"));
                AutoSaveStatusText.Foreground = System.Windows.Media.Brushes.Gray;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Localization.T("Attendance.Save.Failed"), ex.Message), Localization.T("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_selected == null) return;
            if (MessageBox.Show(Localization.T("Attendance.Delete.Confirm"), Localization.T("Common.Confirm"), MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            try
            {
                AttendanceRepository.Delete(_selected.Id);
                LoadAttendance();
                New_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Localization.T("Attendance.Delete.Failed"), ex.Message), Localization.T("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportExcel_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = Localization.T("Attendance.Dialog.OpenTitle"),
                Filter = Localization.T("Attendance.Dialog.Filter.ExcelFiles")
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                var result = AttendanceExcelImporter.Import(dialog.FileName);
                LoadAttendance();

                var summary = new StringBuilder();
                summary.AppendLine(string.Format(Localization.T("Attendance.Import.RowsProcessed"), result.TotalRows));
                summary.AppendLine(string.Format(Localization.T("Attendance.Import.Inserted"), result.Inserted));
                summary.AppendLine(string.Format(Localization.T("Attendance.Import.Updated"), result.Updated));
                summary.AppendLine(string.Format(Localization.T("Attendance.Import.Errors"), result.Errors.Count));

                if (result.Errors.Count > 0)
                {
                    summary.AppendLine();
                    summary.AppendLine(Localization.T("Attendance.Import.FirstIssues"));
                    foreach (var err in result.Errors.Take(15))
                        summary.AppendLine(string.Format(Localization.T("Attendance.Import.RowIssue"), err.RowNumber, err.Message));
                    if (result.Errors.Count > 15)
                        summary.AppendLine(string.Format(Localization.T("Attendance.Import.AndMore"), result.Errors.Count - 15));
                }

                MessageBox.Show(summary.ToString(), Localization.T("Attendance.Import.CompleteTitle"), MessageBoxButton.OK,
                    result.Errors.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Localization.T("Attendance.Import.Failure"), ex.Message), Localization.T("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DownloadTemplate_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title = Localization.T("Attendance.Dialog.SaveTemplateTitle"),
                Filter = Localization.T("Attendance.Dialog.Filter.ExcelFiles"),
                FileName = "Attendance_Import_Template.xlsx"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                AttendanceExcelImporter.WriteTemplate(dialog.FileName);
                MessageBox.Show(Localization.T("Attendance.Template.Saved"), "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Localization.T("Attendance.Error.CouldNotSaveTemplate"), ex.Message), Localization.T("Attendance.Error.Title"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StatusBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void PrintReport_Click(object sender, RoutedEventArgs e)
        {
            var window = new AttendanceReportWindow { Owner = Window.GetWindow(this) };
            window.ShowDialog();
        }
    }
}
