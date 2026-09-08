using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using HR_ERP.Data;
using HR_ERP.Helpers;
using HR_ERP.Models;
using Localization = HR_ERP.Helpers.Localization;

namespace HR_ERP.Views
{
    public partial class ShiftsView : UserControl
    {
        private Shift? _selectedShift;
        private WorkShift? _selectedWorkShift;

        public ShiftsView()
        {
            InitializeComponent();
            ApplyLocalization();
            LoadShifts();
            LoadWorkShifts();
        }

        private void ApplyLocalization()
        {
            ShiftsTitleText.Text = Localization.T("Shifts.Title");
            ColShiftName.Header = Localization.T("Shifts.Col.Name");
            ColShiftStart.Header = Localization.T("Shifts.Col.Start");
            ColShiftEnd.Header = Localization.T("Shifts.Col.End");
            ColShiftLateTol.Header = Localization.T("Shifts.Col.LateTol");
            ShiftNameLabel.Content = Localization.T("Shifts.Field.Name");
            ShiftHoursLabel.Content = Localization.T("Shifts.Field.WorkingHours");
            ShiftStartLabel.Content = Localization.T("Shifts.Field.StartTime");
            ShiftEndLabel.Content = Localization.T("Shifts.Field.EndTime");
            LateTolLabel.Content = Localization.T("Shifts.Field.LateTol");
            EarlyTolLabel.Content = Localization.T("Shifts.Field.EarlyTol");
            OtGraceLabel.Content = Localization.T("Shifts.Field.OtGrace");
            DetectStartLabel.Content = Localization.T("Shifts.Field.DetectStart");
            DetectEndLabel.Content = Localization.T("Shifts.Field.DetectEnd");
            DetectionNoteText.Text = Localization.T("Shifts.Field.DetectionNote");
            ShiftNewButton.Content = Localization.T("Shifts.Button.New");
            ShiftSaveButton.Content = Localization.T("Shifts.Button.Save");
            ShiftDeleteButton.Content = Localization.T("Shifts.Button.Delete");

            WsTitleText.Text = Localization.T("Shifts.Ws.Title");
            ColWsName.Header = Localization.T("Shifts.Col.Name");
            ColWsStart.Header = Localization.T("Shifts.Col.Start");
            ColWsEnd.Header = Localization.T("Shifts.Col.End");
            ColWsTotalHrs.Header = Localization.T("Shifts.Ws.Col.TotalHrs");
            WsNameLabel.Content = Localization.T("Shifts.Ws.Field.Name");
            WsBreakLabel.Content = Localization.T("Shifts.Ws.Field.Break");
            WsStartLabel.Content = Localization.T("Shifts.Field.StartTime");
            WsEndLabel.Content = Localization.T("Shifts.Field.EndTime");
            WsTotalHoursLabel.Content = Localization.T("Shifts.Ws.Field.TotalHours");
            WsNewButton.Content = Localization.T("Shifts.Button.New");
            WsSaveButton.Content = Localization.T("Shifts.Button.Save");
            WsDeleteButton.Content = Localization.T("Shifts.Button.Delete");
        }

        private static TimeSpan? ParseTime(string text) =>
            TimeSpan.TryParseExact(text.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out var t) ? t : null;

        // ---------- Shifts ----------

        private void LoadShifts()
        {
            try { ShiftsGrid.ItemsSource = ShiftRepository.GetAll(); }
            catch (Exception ex) { MessageBox.Show("Could not load shifts: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void ShiftsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ShiftsGrid.SelectedItem is not Shift s) return;
            _selectedShift = s;
            ShiftNameBox.Text = s.ShiftName;
            ShiftHoursBox.Text = s.WorkingHours?.ToString() ?? "";
            ShiftStartBox.Text = s.StartTime.ToString(@"hh\:mm");
            ShiftEndBox.Text = s.EndTime.ToString(@"hh\:mm");
            LateTolBox.Text = s.LateToleranceMinutes.ToString();
            EarlyTolBox.Text = s.EarlyLeaveToleranceMinutes.ToString();
            OtGraceBox.Text = s.OvertimeGraceMinutes.ToString();
            DetectStartBox.Text = s.DetectionStartTime?.ToString(@"hh\:mm") ?? "";
            DetectEndBox.Text = s.DetectionEndTime?.ToString(@"hh\:mm") ?? "";
        }

        private void ShiftNew_Click(object sender, RoutedEventArgs e)
        {
            _selectedShift = null;
            ShiftNameBox.Text = ShiftHoursBox.Text = ShiftStartBox.Text = ShiftEndBox.Text = "";
            LateTolBox.Text = EarlyTolBox.Text = OtGraceBox.Text = "0";
            DetectStartBox.Text = DetectEndBox.Text = "";
            ShiftsGrid.SelectedItem = null;
        }

        private void ShiftSave_Click(object sender, RoutedEventArgs e)
        {
            var start = ParseTime(ShiftStartBox.Text);
            var end = ParseTime(ShiftEndBox.Text);
            if (string.IsNullOrWhiteSpace(ShiftNameBox.Text) || start == null || end == null)
            {
                MessageBox.Show("Shift name, start time and end time (HH:mm) are required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var detectStart = ParseTime(DetectStartBox.Text);
            var detectEnd = ParseTime(DetectEndBox.Text);
            if ((detectStart == null) != (detectEnd == null))
            {
                MessageBox.Show(Localization.T("Shifts.Validation.DetectionBothOrNeither"), "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var shift = new Shift
            {
                ShiftID = _selectedShift?.ShiftID ?? 0,
                ShiftName = ShiftNameBox.Text.Trim(),
                StartTime = start.Value,
                EndTime = end.Value,
                LateToleranceMinutes = int.TryParse(LateTolBox.Text, out var lt) ? lt : 0,
                EarlyLeaveToleranceMinutes = int.TryParse(EarlyTolBox.Text, out var et) ? et : 0,
                OvertimeGraceMinutes = int.TryParse(OtGraceBox.Text, out var og) ? og : 0,
                WorkingHours = int.TryParse(ShiftHoursBox.Text, out var wh) ? wh : null,
                DetectionStartTime = detectStart,
                DetectionEndTime = detectEnd
            };

            try
            {
                if (_selectedShift == null) ShiftRepository.Add(shift);
                else ShiftRepository.Update(shift);
                LoadShifts();
                ShiftNew_Click(sender, e);
            }
            catch (Exception ex) { MessageBox.Show("Save failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void ShiftDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedShift == null) return;
            if (MessageBox.Show($"Delete shift '{_selectedShift.ShiftName}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            try
            {
                ShiftRepository.Delete(_selectedShift.ShiftID);
                LoadShifts();
                ShiftNew_Click(sender, e);
            }
            catch (Exception ex) { MessageBox.Show("Delete failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        // ---------- WorkShifts ----------

        private void LoadWorkShifts()
        {
            try { WorkShiftsGrid.ItemsSource = WorkShiftRepository.GetAll(); }
            catch (Exception ex) { MessageBox.Show("Could not load work shift templates: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void WorkShiftsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (WorkShiftsGrid.SelectedItem is not WorkShift w) return;
            _selectedWorkShift = w;
            WsNameBox.Text = w.Name;
            WsBreakBox.Text = w.BreakMinutes.ToString();
            WsStartBox.Text = w.StartTime.ToString(@"hh\:mm");
            WsEndBox.Text = w.EndTime.ToString(@"hh\:mm");
            WsTotalHoursBox.Text = w.TotalHours?.ToString() ?? "";
        }

        private void WsNew_Click(object sender, RoutedEventArgs e)
        {
            _selectedWorkShift = null;
            WsNameBox.Text = WsStartBox.Text = WsEndBox.Text = WsTotalHoursBox.Text = "";
            WsBreakBox.Text = "0";
            WorkShiftsGrid.SelectedItem = null;
        }

        private void WsSave_Click(object sender, RoutedEventArgs e)
        {
            var start = ParseTime(WsStartBox.Text);
            var end = ParseTime(WsEndBox.Text);
            if (string.IsNullOrWhiteSpace(WsNameBox.Text) || start == null || end == null)
            {
                MessageBox.Show("Name, start time and end time (HH:mm) are required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ws = new WorkShift
            {
                Id = _selectedWorkShift?.Id ?? 0,
                Name = WsNameBox.Text.Trim(),
                StartTime = start.Value,
                EndTime = end.Value,
                BreakMinutes = int.TryParse(WsBreakBox.Text, out var b) ? b : 0,
                TotalHours = decimal.TryParse(WsTotalHoursBox.Text, out var th) ? th : null
            };

            try
            {
                if (_selectedWorkShift == null) WorkShiftRepository.Add(ws);
                else WorkShiftRepository.Update(ws);
                LoadWorkShifts();
                WsNew_Click(sender, e);
            }
            catch (Exception ex) { MessageBox.Show("Save failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void WsDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedWorkShift == null) return;
            if (MessageBox.Show($"Delete template '{_selectedWorkShift.Name}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            try
            {
                WorkShiftRepository.Delete(_selectedWorkShift.Id);
                LoadWorkShifts();
                WsNew_Click(sender, e);
            }
            catch (Exception ex) { MessageBox.Show("Delete failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }
}
