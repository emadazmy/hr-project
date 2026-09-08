using System.IO;
using System.Windows;
using System.Windows.Controls;
using Localization = HR_ERP.Helpers.Localization;
using HR_ERP.Data;
using HR_ERP.Models;
using Microsoft.Win32;

namespace HR_ERP.Views
{
    /// <summary>Add/Edit Employee, opened by EmployeesView's New/Edit buttons. Main identity
    /// fields (Code/Name/Department/Position/Status) stay visible at the top no matter which
    /// tab is open; Details/Documents/History/Salary are tabs below that. Documents, History and
    /// Salary all need an existing employee code, so those three tabs stay disabled until the
    /// employee has been saved at least once (immediately, for New — the window switches itself
    /// into edit mode after the first successful Save rather than closing).</summary>
    public partial class EmployeeDetailWindow : Window
    {
        private List<Department> _departments = new();
        private Employee? _original;   // the record as loaded from the DB (null until first save, in New mode)
        private EmployeeDocument? _selectedDocument;

        /// <summary>True if anything was actually saved — tells EmployeesView it needs to
        /// reload the grid after this window closes.</summary>
        public bool Saved { get; private set; }

        public EmployeeDetailWindow(Employee? employee)
        {
            InitializeComponent();
            ApplyLocalization();
            LoadDepartments();

            _original = employee;
            if (_original != null)
                PopulateFromEmployee(_original);
            else
                ResetForNew();

            UpdateTabAvailability();
        }

        private void ApplyLocalization()
        {
            FlowDirection = Localization.FlowDirection;
            Title = Localization.T("Employees.Detail.WindowTitle");
            TitleText.Text = Localization.T("Employees.Detail.WindowTitle");
            MainDataHeader.Text = Localization.T("Employees.Detail.MainDataHeader");

            LabelEmployeeCode.Content = Localization.T("Employees.Edit.EmployeeCode");
            LabelFullName.Content = Localization.T("Employees.Edit.FullName");
            LabelDepartment.Content = Localization.T("Employees.Edit.Department");
            LabelPosition.Content = Localization.T("Employees.Edit.Position");
            LabelEmploymentStatus.Content = Localization.T("Employees.Edit.EmploymentStatus");

            DetailsTab.Header = Localization.T("Employees.Detail.Tab.Details");
            DocumentsTab.Header = Localization.T("Employees.Detail.Tab.Documents");
            HistoryTab.Header = Localization.T("Employees.Detail.Tab.History");
            SalaryTab.Header = Localization.T("Employees.Detail.Tab.Salary");

            LabelGender.Content = Localization.T("Employees.Edit.Gender");
            LabelDob.Content = Localization.T("Employees.Edit.Dob");
            LabelEmail.Content = Localization.T("Employees.Edit.Email");
            LabelPhone.Content = Localization.T("Employees.Edit.Phone");
            LabelHireDate.Content = Localization.T("Employees.Edit.HireDate");
            LabelNationalId.Content = Localization.T("Employees.Edit.NationalId");
            LabelEmploymentType.Content = Localization.T("Employees.Edit.EmploymentType");
            LabelInsuranceStatus.Content = Localization.T("Employees.Edit.InsuranceStatus");
            LabelAddress.Content = Localization.T("Employees.Edit.Address");
            LabelWeekend1.Content = Localization.T("Employees.Edit.Weekend1");
            LabelWeekend2.Content = Localization.T("Employees.Edit.Weekend2");
            LabelMarital.Content = Localization.T("Employees.Edit.MaritalStatus");
            LabelAnnualLeaveDays.Content = Localization.T("Employees.Edit.AnnualLeaveDays");
            LabelLeaveCarriedOver.Content = Localization.T("Employees.Edit.LeaveCarriedOver");
            LabelReportsTo.Content = Localization.T("Employees.Edit.ReportsTo");

            if (GenderBox != null)
            {
                var items = GenderBox.Items.OfType<ComboBoxItem>().ToList();
                if (items.Count >= 2)
                {
                    items[0].Content = Localization.T("Gender.Male"); items[0].Tag = "Male";
                    items[1].Content = Localization.T("Gender.Female"); items[1].Tag = "Female";
                }
            }
            if (EmploymentTypeBox != null)
            {
                var items = EmploymentTypeBox.Items.OfType<ComboBoxItem>().ToList();
                if (items.Count >= 3)
                {
                    items[0].Content = Localization.T("EmploymentType.FullTime"); items[0].Tag = "Full-time";
                    items[1].Content = Localization.T("EmploymentType.PartTime"); items[1].Tag = "Part-time";
                    items[2].Content = Localization.T("EmploymentType.Contract"); items[2].Tag = "Contract";
                }
            }
            if (EmploymentStatusBox != null)
            {
                var items = EmploymentStatusBox.Items.OfType<ComboBoxItem>().ToList();
                if (items.Count >= 2)
                {
                    items[0].Content = Localization.T("EmploymentStatus.Active"); items[0].Tag = "Active";
                    items[1].Content = Localization.T("EmploymentStatus.Terminated"); items[1].Tag = "Terminated";
                }
            }
            if (InsuranceStatusBox != null)
            {
                var items = InsuranceStatusBox.Items.OfType<ComboBoxItem>().ToList();
                if (items.Count >= 2)
                {
                    items[0].Content = Localization.T("InsuranceStatus.Insured"); items[0].Tag = "Insured";
                    items[1].Content = Localization.T("InsuranceStatus.NotInsured"); items[1].Tag = "NotInsured";
                }
            }

            DocumentsHintText.Text = Localization.T("Employees.Documents.Hint");
            DocumentsDescriptionText.Text = Localization.T("Employees.Documents.Description");
            ColDocCategory.Header = Localization.T("Employees.Documents.Column.Category");
            ColDocFileName.Header = Localization.T("Employees.Documents.Column.FileName");
            ColDocDescription.Header = Localization.T("Employees.Documents.Column.Description");
            ColDocUploaded.Header = Localization.T("Employees.Documents.Column.Uploaded");
            DocumentTypeBox.ToolTip = Localization.T("Employees.Documents.TypeTooltip");
            DocumentDescriptionBox.ToolTip = Localization.T("Employees.Documents.DescriptionTooltip");
            ImportDocumentButton.Content = Localization.T("Employees.Documents.ImportButton");
            ViewDocumentButton.Content = Localization.T("Employees.Documents.ViewButton");
            DeleteDocumentButton.Content = Localization.T("Employees.Documents.DeleteButton");

            HistoryHintText.Text = Localization.T("Employees.History.Hint");
            HistoryDescriptionText.Text = Localization.T("Employees.History.Description");
            ColHistDate.Header = Localization.T("Employees.History.Column.Date");
            ColHistEvent.Header = Localization.T("Employees.History.Column.Event");
            ColHistChange.Header = Localization.T("Employees.History.Column.Change");
            ColHistNotes.Header = Localization.T("Employees.History.Column.Notes");
            ColHistRecorded.Header = Localization.T("Employees.History.Column.Recorded");
            HistoryEventLabel.Content = Localization.T("Employees.History.EventType");
            HistoryEventDateLabel.Content = Localization.T("Employees.History.EventDate");
            HistoryPerformanceLabel.Content = Localization.T("Employees.History.Performance");
            HistoryNotesLabel.Content = Localization.T("Employees.History.Notes");
            var ratingItems = HistoryRatingBox?.Items.OfType<ComboBoxItem>().ToList();
            if (ratingItems != null && ratingItems.Count >= 4)
            {
                ratingItems[0].Content = Localization.T("Employees.History.Rating.Excellent");
                ratingItems[1].Content = Localization.T("Employees.History.Rating.Good");
                ratingItems[2].Content = Localization.T("Employees.History.Rating.Average");
                ratingItems[3].Content = Localization.T("Employees.History.Rating.NeedsImprovement");
            }
            AddHistoryButton.Content = Localization.T("Employees.History.AddButton");
            DeleteHistoryButton.Content = Localization.T("Employees.History.DeleteButton");

            SalaryHintText.Text = Localization.T("Employees.Salary.Hint");
            SalaryEntitlementsHeader.Text = Localization.T("Employees.Salary.EntitlementsHeader");
            SalaryBasicLabel.Content = Localization.T("Employees.Salary.Basic");
            SalaryFixedAllowanceLabel.Content = Localization.T("Employees.Salary.FixedAllowance");
            SalaryCompletionLabel.Content = Localization.T("Employees.Salary.Completion");
            SalaryOtherEntitlementsLabel.Content = Localization.T("Employees.Salary.OtherEntitlements");
            SalaryDeductionsHeader.Text = Localization.T("Employees.Salary.DeductionsHeader");
            SalaryInsuranceAmountLabel.Content = Localization.T("Employees.Salary.InsuranceAmount");
            SalaryMonthlyTaxLabel.Content = Localization.T("Employees.Salary.MonthlyTax");
            SalaryInsuranceNoteText.Text = Localization.T("Employees.Salary.InsuranceNote");
            SalaryRatesHeader.Text = Localization.T("Employees.Salary.RatesHeader");
            SalaryOvertimeRateLabel.Content = Localization.T("Employees.Salary.OvertimeRate");
            SalaryLateDeductionRateLabel.Content = Localization.T("Employees.Salary.LateDeductionRate");
            SaveSalaryButton.Content = Localization.T("Employees.Salary.SaveButton");
            SalaryHistoryHeader.Text = Localization.T("Employees.Salary.HistoryHeader");
            ColSalaryHistDate.Header = Localization.T("Employees.Salary.History.Date");
            ColSalaryHistBasic.Header = Localization.T("Employees.Salary.Basic");
            ColSalaryHistAllowance.Header = Localization.T("Employees.Salary.FixedAllowance");
            ColSalaryHistCompletion.Header = Localization.T("Employees.Salary.Completion");
            ColSalaryHistOther.Header = Localization.T("Employees.Salary.OtherEntitlements");
            ColSalaryHistInsurance.Header = Localization.T("Employees.Salary.InsuranceAmount");
            ColSalaryHistTax.Header = Localization.T("Employees.Salary.MonthlyTax");
            ColSalaryHistInsured.Header = Localization.T("Employees.Salary.History.Insured");
            ColSalaryHistBy.Header = Localization.T("Employees.Salary.History.ChangedBy");

            SaveButton.Content = Localization.T("Employees.Button.Save");
            CloseButton.Content = Localization.T("Common.Cancel");

            RefreshDocumentCategoryOptions();
            RefreshHistoryEventTypeOptions();
        }

        private void RefreshDocumentCategoryOptions()
        {
            var currentValue = (DocumentTypeBox.SelectedItem as ComboOption)?.Value;
            var options = EmployeeDocument.CategoryOptions;
            DocumentTypeBox.ItemsSource = options;
            DocumentTypeBox.SelectedItem = options.FirstOrDefault(o => o.Value == currentValue) ?? options.FirstOrDefault();
        }

        private void RefreshHistoryEventTypeOptions()
        {
            var currentValue = (HistoryEventTypeBox.SelectedItem as ComboOption)?.Value;
            var options = EmploymentHistoryEntry.EventTypeOptions;
            HistoryEventTypeBox.ItemsSource = options;
            HistoryEventTypeBox.SelectedItem = currentValue == null ? null : options.FirstOrDefault(o => o.Value == currentValue);
        }

        private void LoadDepartments()
        {
            _departments = DepartmentRepository.GetAll();
            DepartmentBox.ItemsSource = _departments;
        }

        /// <summary>Populates the "Reports To" picker with every employee except the one
        /// currently being edited (an employee can't report to themselves).</summary>
        private void LoadManagerOptions()
        {
            try
            {
                var all = EmployeeRepository.GetAll();
                ManagerBox.ItemsSource = _original == null ? all : all.Where(x => x.Code != _original.Code).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load manager list: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DepartmentBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DepartmentBox.SelectedItem is Department dep)
                PositionBox.ItemsSource = JobPositionRepository.GetByDepartment(dep.Name);
        }

        /// <summary>Documents/History/Salary all need an employee row to already exist (they're
        /// keyed by employee code), so they stay disabled for a brand-new, not-yet-saved
        /// employee and enable themselves the moment Save creates that row.</summary>
        private void UpdateTabAvailability()
        {
            bool hasEmployee = _original != null;
            DocumentsTab.IsEnabled = hasEmployee;
            HistoryTab.IsEnabled = hasEmployee;
            SalaryTab.IsEnabled = hasEmployee;
        }

        private void PopulateFromEmployee(Employee emp)
        {
            CodeBox.Text = emp.Code;
            CodeBox.IsEnabled = false; // code is the primary key once created — never editable afterward
            NameBox.Text = emp.Name;
            SetComboText(EmploymentStatusBox, emp.EmploymentStatus);

            SetComboText(GenderBox, emp.Gender);
            DobPicker.SelectedDate = emp.DateOfBirth;
            EmailBox.Text = emp.Email;
            PhoneBox.Text = emp.Phone;
            HireDatePicker.SelectedDate = emp.HireDate;
            NationalIdBox.Text = emp.NationalId;
            AddressBox.Text = emp.Address;
            Weekend1Box.Text = emp.Weekend1;
            Weekend2Box.Text = emp.Weekend2;
            MaritalBox.Text = emp.MaritalStatus;
            AnnualLeaveDaysBox.Text = emp.AnnualLeaveDays.ToString();
            LeaveCarriedOverBox.Text = emp.LeaveCarriedOver.ToString();
            SetComboText(EmploymentTypeBox, emp.EmploymentType);
            SetComboText(InsuranceStatusBox, emp.IsInsured ? "Insured" : "NotInsured");

            LoadManagerOptions();
            ManagerBox.SelectedItem = (ManagerBox.ItemsSource as List<Employee>)?.FirstOrDefault(x => x.Code == emp.ManagerCode);

            if (!string.IsNullOrWhiteSpace(emp.Depart))
            {
                DepartmentBox.SelectedItem = _departments.FirstOrDefault(d => d.Name == emp.Depart);
                var positions = JobPositionRepository.GetByDepartment(emp.Depart);
                PositionBox.ItemsSource = positions;
                PositionBox.SelectedItem = positions.FirstOrDefault(p => p.Title == emp.Position);
            }

            LoadDocuments();
            LoadHistory();
            LoadSalaryFields(emp);
            LoadSalaryHistory(emp.Code);
        }

        private void ResetForNew()
        {
            CodeBox.Text = NameBox.Text = EmailBox.Text = PhoneBox.Text = NationalIdBox.Text = AddressBox.Text = "";
            CodeBox.IsEnabled = true;
            Weekend1Box.Text = Weekend2Box.Text = MaritalBox.Text = "";
            AnnualLeaveDaysBox.Text = "21";
            LeaveCarriedOverBox.Text = "0";
            GenderBox.SelectedItem = null;
            EmploymentTypeBox.SelectedItem = null;
            EmploymentStatusBox.SelectedIndex = 0;
            DobPicker.SelectedDate = null;
            HireDatePicker.SelectedDate = DateTime.Today;
            DepartmentBox.SelectedItem = null;
            PositionBox.ItemsSource = null;
            InsuranceStatusBox.SelectedIndex = 0;
            LoadManagerOptions();
            ManagerBox.SelectedItem = null;
            ResetSalaryFields();
            DocumentsGrid.ItemsSource = null;
            HistoryGrid.ItemsSource = null;
            DocumentsHintText.Visibility = Visibility.Visible;
            HistoryHintText.Visibility = Visibility.Visible;
        }

        // ---------------- Personal Documents Hub ----------------

        private void LoadDocuments()
        {
            if (_original == null) { DocumentsGrid.ItemsSource = null; DocumentsHintText.Visibility = Visibility.Visible; return; }
            DocumentsHintText.Visibility = Visibility.Collapsed;
            try
            {
                DocumentsGrid.ItemsSource = EmployeeDocumentRepository.GetByEmployee(_original.Code);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Localization.T("Employees.Error.CouldNotLoadDocuments"), ex.Message), Localization.T("Attendance.Error.Title"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DocumentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => _selectedDocument = DocumentsGrid.SelectedItem as EmployeeDocument;

        private void ImportDocument_Click(object sender, RoutedEventArgs e)
        {
            if (_original == null) return; // tab is disabled in this state, but guard anyway

            var dialog = new OpenFileDialog
            {
                Title = Localization.T("Employees.Dialog.SelectDocumentTitle"),
                Filter = Localization.T("Employees.Dialog.Filter.DocumentFiles")
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                var storedPath = DocumentStorage.CopyIntoStorage(_original.Code, dialog.FileName);
                EmployeeDocumentRepository.Add(new EmployeeDocument
                {
                    EmployeeCode = _original.Code,
                    DocumentType = (DocumentTypeBox.SelectedItem as ComboOption)?.Value ?? "Other",
                    FileName = Path.GetFileName(dialog.FileName),
                    StoredPath = storedPath,
                    Description = DocumentDescriptionBox.Text.Trim()
                });

                DocumentDescriptionBox.Text = "";
                LoadDocuments();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Localization.T("Employees.Error.ImportFailed"), ex.Message), Localization.T("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DocumentsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DocumentsGrid.SelectedItem is EmployeeDocument) ViewDocument_Click(sender, new RoutedEventArgs());
        }

        private void ViewDocument_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDocument == null)
            {
                MessageBox.Show(Localization.T("Employees.Info.SelectDocumentFirst"), Localization.T("Common.Info"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            try
            {
                var window = new DocumentViewerWindow(_selectedDocument) { Owner = this };
                window.ShowDialog();
                LoadDocuments();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open document: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteDocument_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDocument == null)
            {
                MessageBox.Show("Select a document first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (MessageBox.Show($"Delete '{_selectedDocument.FileName}'? This also removes the stored copy from disk.",
                "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            try
            {
                EmployeeDocumentRepository.Delete(_selectedDocument.Id);
                _selectedDocument = null;
                LoadDocuments();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Localization.T("Employees.Error.DeleteDocumentFailed"), ex.Message), Localization.T("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------------- Employment History ----------------

        private void LoadHistory()
        {
            if (_original == null) { HistoryGrid.ItemsSource = null; HistoryHintText.Visibility = Visibility.Visible; return; }
            HistoryHintText.Visibility = Visibility.Collapsed;
            try
            {
                HistoryGrid.ItemsSource = EmploymentHistoryRepository.GetByEmployee(_original.Code);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load employment history: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddHistoryEntry_Click(object sender, RoutedEventArgs e)
        {
            if (_original == null) return;
            if (HistoryEventTypeBox.SelectedItem is not ComboOption eventTypeOption)
            {
                MessageBox.Show("Select an event type.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                EmploymentHistoryRepository.Add(new EmploymentHistoryEntry
                {
                    EmployeeCode = _original.Code,
                    EventDate = HistoryDatePicker.SelectedDate ?? DateTime.Today,
                    EventType = eventTypeOption.Value,
                    PerformanceRating = (HistoryRatingBox.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                    Notes = HistoryNotesBox.Text.Trim()
                });

                HistoryNotesBox.Text = "";
                HistoryRatingBox.SelectedItem = null;
                LoadHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Add failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteHistoryEntry_Click(object sender, RoutedEventArgs e)
        {
            if (HistoryGrid.SelectedItem is not EmploymentHistoryEntry entry)
            {
                MessageBox.Show("Select a history entry first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (MessageBox.Show("Delete this history entry?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            try
            {
                EmploymentHistoryRepository.Delete(entry.Id);
                LoadHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Delete failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------------- Salary Details ----------------

        private void LoadSalaryFields(Employee emp)
        {
            SalaryBasicBox.Text = emp.BasicSalary.ToString();
            SalaryFixedAllowanceBox.Text = emp.Allowance.ToString();
            SalaryCompletionBox.Text = emp.SalaryCompletion.ToString();
            SalaryOtherEntitlementsBox.Text = emp.OtherEntitlements.ToString();
            SalaryInsuranceAmountBox.Text = emp.SocialInsuranceAmount.ToString();
            SalaryMonthlyTaxBox.Text = emp.MonthlyTax.ToString();
            SalaryOvertimeRateBox.Text = emp.OvertimeRate.ToString();
            SalaryLateDeductionRateBox.Text = emp.LateDeductionRate.ToString();
            SalaryHintText.Visibility = Visibility.Collapsed;
        }

        private void ResetSalaryFields()
        {
            SalaryBasicBox.Text = SalaryFixedAllowanceBox.Text = SalaryCompletionBox.Text = SalaryOtherEntitlementsBox.Text = "0";
            SalaryInsuranceAmountBox.Text = SalaryMonthlyTaxBox.Text = "0";
            SalaryOvertimeRateBox.Text = SalaryLateDeductionRateBox.Text = "0";
            SalaryHistoryGrid.ItemsSource = null;
            SalaryHintText.Visibility = Visibility.Visible;
        }

        private void LoadSalaryHistory(string employeeCode)
        {
            try
            {
                SalaryHistoryGrid.ItemsSource = SalaryHistoryRepository.GetByEmployee(employeeCode);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load salary history: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSalary_Click(object sender, RoutedEventArgs e)
        {
            if (_original == null) return; // tab disabled in this state, but guard anyway

            var updated = new Employee
            {
                Code = _original.Code,
                BasicSalary = decimal.TryParse(SalaryBasicBox.Text, out var bs) ? bs : 0,
                Allowance = decimal.TryParse(SalaryFixedAllowanceBox.Text, out var al) ? al : 0,
                SalaryCompletion = decimal.TryParse(SalaryCompletionBox.Text, out var sc) ? sc : 0,
                OtherEntitlements = decimal.TryParse(SalaryOtherEntitlementsBox.Text, out var oe) ? oe : 0,
                SocialInsuranceAmount = decimal.TryParse(SalaryInsuranceAmountBox.Text, out var sia) ? sia : 0,
                MonthlyTax = decimal.TryParse(SalaryMonthlyTaxBox.Text, out var mt) ? mt : 0,
                OvertimeRate = decimal.TryParse(SalaryOvertimeRateBox.Text, out var otr) ? otr : 0,
                LateDeductionRate = decimal.TryParse(SalaryLateDeductionRateBox.Text, out var ldr) ? ldr : 0
            };

            try
            {
                EmployeeRepository.UpdateSalaryFields(updated);

                SalaryHistoryRepository.Add(new SalaryHistoryEntry
                {
                    EmployeeCode = _original.Code,
                    BasicSalary = updated.BasicSalary,
                    FixedAllowance = updated.Allowance,
                    SalaryCompletion = updated.SalaryCompletion,
                    OtherEntitlements = updated.OtherEntitlements,
                    IsInsured = _original.IsInsured,
                    SocialInsuranceAmount = updated.SocialInsuranceAmount,
                    MonthlyTax = updated.MonthlyTax,
                    ChangedBy = HR_ERP.Helpers.Session.CurrentUser?.Username
                });

                _original.BasicSalary = updated.BasicSalary;
                _original.Allowance = updated.Allowance;
                _original.SalaryCompletion = updated.SalaryCompletion;
                _original.OtherEntitlements = updated.OtherEntitlements;
                _original.SocialInsuranceAmount = updated.SocialInsuranceAmount;
                _original.MonthlyTax = updated.MonthlyTax;
                _original.OvertimeRate = updated.OvertimeRate;
                _original.LateDeductionRate = updated.LateDeductionRate;

                Saved = true;
                LoadSalaryHistory(_original.Code);
                MessageBox.Show("Salary details saved.", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>Compares the previously-loaded record against what's about to be saved and
        /// logs Department Transfer / Position Change / Salary Change entries automatically.</summary>
        private void LogAutomaticHistory(Employee before, Employee after)
        {
            var today = DateTime.Today;

            if (!string.Equals(before.Depart, after.Depart, StringComparison.OrdinalIgnoreCase))
            {
                EmploymentHistoryRepository.Add(new EmploymentHistoryEntry
                {
                    EmployeeCode = after.Code, EventDate = today, EventType = "Department Transfer",
                    OldDepartment = before.Depart, NewDepartment = after.Depart
                });
            }

            if (!string.Equals(before.Position, after.Position, StringComparison.OrdinalIgnoreCase))
            {
                EmploymentHistoryRepository.Add(new EmploymentHistoryEntry
                {
                    EmployeeCode = after.Code, EventDate = today, EventType = "Position Change",
                    OldPosition = before.Position, NewPosition = after.Position
                });
            }

            if (before.BasicSalary != after.BasicSalary)
            {
                EmploymentHistoryRepository.Add(new EmploymentHistoryEntry
                {
                    EmployeeCode = after.Code, EventDate = today, EventType = "Salary Change",
                    OldSalary = before.BasicSalary, NewSalary = after.BasicSalary
                });
            }

            if (!string.Equals(before.EmploymentStatus, after.EmploymentStatus, StringComparison.OrdinalIgnoreCase)
                && string.Equals(after.EmploymentStatus, "Terminated", StringComparison.OrdinalIgnoreCase))
            {
                EmploymentHistoryRepository.Add(new EmploymentHistoryEntry
                {
                    EmployeeCode = after.Code, EventDate = today, EventType = "Termination"
                });
            }
        }

        private static void SetComboText(ComboBox box, string? value)
        {
            foreach (ComboBoxItem item in box.Items)
            {
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
            box.SelectedItem = null;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CodeBox.Text) || string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Employee Code and Name are required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var managerCode = (ManagerBox.SelectedItem as Employee)?.Code;
            if (!string.IsNullOrWhiteSpace(managerCode) && _original != null)
            {
                var manager = (ManagerBox.ItemsSource as List<Employee>)?.FirstOrDefault(x => x.Code == managerCode);
                if (manager?.ManagerCode == _original.Code)
                {
                    MessageBox.Show("This would create a circular reporting line (the selected manager already reports to this employee).",
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var emp = new Employee
            {
                Id = _original?.Id ?? 0,
                Code = CodeBox.Text.Trim(),
                Name = NameBox.Text.Trim(),
                Gender = (GenderBox.SelectedItem as ComboBoxItem)?.Tag?.ToString(),
                DateOfBirth = DobPicker.SelectedDate,
                Email = EmailBox.Text.Trim(),
                Phone = PhoneBox.Text.Trim(),
                NationalId = NationalIdBox.Text.Trim(),
                Address = AddressBox.Text.Trim(),
                HireDate = HireDatePicker.SelectedDate ?? DateTime.Today,
                Depart = (DepartmentBox.SelectedItem as Department)?.Name,
                Position = (PositionBox.SelectedItem as JobPosition)?.Title,
                EmploymentType = (EmploymentTypeBox.SelectedItem as ComboBoxItem)?.Tag?.ToString(),
                EmploymentStatus = (EmploymentStatusBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Active",
                Weekend1 = Weekend1Box.Text.Trim(),
                Weekend2 = Weekend2Box.Text.Trim(),
                MaritalStatus = MaritalBox.Text.Trim(),
                BasicSalary = _original?.BasicSalary ?? 0,
                Allowance = _original?.Allowance ?? 0,
                OvertimeRate = _original?.OvertimeRate ?? 0,
                LateDeductionRate = _original?.LateDeductionRate ?? 0,
                SalaryCompletion = _original?.SalaryCompletion ?? 0,
                OtherEntitlements = _original?.OtherEntitlements ?? 0,
                SocialInsuranceAmount = _original?.SocialInsuranceAmount ?? 0,
                MonthlyTax = _original?.MonthlyTax ?? 0,
                IsInsured = (InsuranceStatusBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() == "Insured",
                AnnualLeaveDays = int.TryParse(AnnualLeaveDaysBox.Text, out var ald) ? ald : 21,
                LeaveCarriedOver = int.TryParse(LeaveCarriedOverBox.Text, out var lco) ? lco : 0,
                ManagerCode = (ManagerBox.SelectedItem as Employee)?.Code
            };

            try
            {
                bool isNew = _original == null;
                if (isNew)
                {
                    EmployeeRepository.Add(emp);
                    EmploymentHistoryRepository.Add(new EmploymentHistoryEntry
                    {
                        EmployeeCode = emp.Code,
                        EventDate = (DateTime)emp.HireDate,
                        EventType = "Hire",
                        NewDepartment = emp.Depart,
                        NewPosition = emp.Position,
                        NewSalary = emp.BasicSalary
                    });
                }
                else
                {
                    LogAutomaticHistory(_original!, emp);
                    EmployeeRepository.Update(emp);
                }

                Saved = true;
                _original = emp;

                if (isNew)
                {
                    Title = Localization.T("Employees.Detail.WindowTitle");
                    CodeBox.IsEnabled = false;
                    UpdateTabAvailability();
                    LoadDocuments();
                    LoadHistory();
                    LoadSalaryFields(emp);
                    LoadSalaryHistory(emp.Code);
                    MessageBox.Show(Localization.T("Employees.Detail.CreatedNote"), Localization.T("Common.Done"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(Localization.T("Employees.Detail.SavedNote"), Localization.T("Common.Done"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
