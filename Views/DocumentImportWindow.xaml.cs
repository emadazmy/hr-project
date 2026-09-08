using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HR_ERP.Helpers;
using Localization = HR_ERP.Helpers.Localization;

namespace HR_ERP.Views
{
    /// <summary>Small modal prompt used both when importing a new file (choose its display
    /// name and category) and when renaming an existing one (same two fields, pre-filled) —
    /// the caller sets the window Title to distinguish the two.</summary>
    public partial class DocumentImportWindow : Window
    {
        private readonly string _extension;

        public string ResultName { get; private set; } = "";
        public string ResultCategory { get; private set; } = "Other";

        public DocumentImportWindow(string suggestedName, string extension, string initialCategory)
        {
            InitializeComponent();
            ApplyLocalization();

            _extension = extension;
            NameBox.Text = suggestedName;
            ExtensionSuffixText.Text = extension;
            SetCategorySelection(initialCategory);

            Loaded += (_, _) =>
            {
                NameBox.Focus();
                NameBox.SelectAll();
            };
        }

        private void ApplyLocalization()
        {
            FlowDirection = Localization.FlowDirection;
            NameLabel.Content = Localization.T("Documents.Field.Name");
            CategoryLabel.Content = Localization.T("Documents.Field.Category");
            CategoryImportantItem.Content = Localization.T("Documents.Category.Important");
            CategoryFreeItem.Content = Localization.T("Documents.Category.Free");
            CategoryOtherItem.Content = Localization.T("Documents.Category.Other");
            OkButton.Content = Localization.T("Common.Ok");
            CancelButton.Content = Localization.T("Common.Cancel");
        }

        private void SetCategorySelection(string category)
        {
            foreach (ComboBoxItem item in CategoryBox.Items)
                if (string.Equals(item.Tag?.ToString(), category, StringComparison.OrdinalIgnoreCase))
                {
                    CategoryBox.SelectedItem = item;
                    return;
                }
            CategoryBox.SelectedIndex = 0;
        }

        private void NameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Ok_Click(sender, e);
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var name = NameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(Localization.T("Documents.Validation.NameRequired"), Localization.T("Common.Validation"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Strip characters that can't appear in a file name — the resulting file is saved
            // to disk under this name plus the original extension.
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            ResultName = name;
            ResultCategory = (CategoryBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Other";
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
