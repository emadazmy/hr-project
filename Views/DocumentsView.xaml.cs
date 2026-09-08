using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using HR_ERP.Data;
using HR_ERP.Helpers;
using HR_ERP.Models;
using Microsoft.Win32;
using Localization = HR_ERP.Helpers.Localization;

namespace HR_ERP.Views
{
    /// <summary>The general-purpose Documents hub — Important Documents / Free Documents /
    /// Other, independent of any single employee (see EmployeesView's own document tab for the
    /// per-employee one). Files are copied into an "important documents" folder next to the app
    /// (DocumentLibraryStorage); this view is just an icon-grid browser over that folder plus
    /// Open/Print/Rename/Delete.</summary>
    public partial class DocumentsView : UserControl
    {
        private List<DocumentLibraryItem> _allDocuments = new();
        private DocumentDisplayItem? _selected;
        private double _iconSize = 100;
        private string _sortMode = "NameAsc";

        public DocumentsView()
        {
            InitializeComponent();
            ApplyLocalization();
            LoadDocuments();
        }

        /// <summary>Translates every label, header and button on this screen, including the
        /// category filter and the Rename/Import dialog's dropdown. The Category *data* value
        /// ("Important"/"Free"/"Other") is deliberately left in English regardless of language —
        /// it's stored as literal text and matched by the category filter — so only what's
        /// displayed changes with the language.</summary>
        private void ApplyLocalization()
        {
            TitleText.Text = Localization.T("Documents.Title");
            ImportButton.Content = Localization.T("Documents.Button.Import");
            IconSizeLabel.Content = Localization.T("Documents.Field.IconSize");

            FilterAllRadio.Content = Localization.T("Documents.Filter.All");
            FilterImportantRadio.Content = Localization.T("Documents.Category.Important");
            FilterFreeRadio.Content = Localization.T("Documents.Category.Free");
            FilterOtherRadio.Content = Localization.T("Documents.Category.Other");

            SortButton.Content = Localization.T("Documents.Sort.Button");
            SortNameAscItem.Header = Localization.T("Documents.Sort.NameAsc");
            SortNameDescItem.Header = Localization.T("Documents.Sort.NameDesc");
            SortDateNewestItem.Header = Localization.T("Documents.Sort.DateNewest");
            SortDateOldestItem.Header = Localization.T("Documents.Sort.DateOldest");
            SortSizeLargestItem.Header = Localization.T("Documents.Sort.SizeLargest");
            SortSizeSmallestItem.Header = Localization.T("Documents.Sort.SizeSmallest");
            SortTypeItem.Header = Localization.T("Documents.Sort.Type");
            UpdateSortMenuChecks();

            NoDocumentsText.Text = Localization.T("Documents.NoDocuments");

            OpenButton.Content = Localization.T("Documents.Button.Open");
            PrintButton.Content = Localization.T("Documents.Button.Print");
            RenameButton.Content = Localization.T("Documents.Button.Rename");
            DeleteButton.Content = Localization.T("Documents.Button.Delete");

            // Import/Rename/Delete change the library; a Viewer-tier role (can_view but not
            // can_edit) can still browse, open and print, but never add, rename, or remove.
            bool canEdit = Session.CanEdit("Documents");
            ImportButton.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            RenameButton.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            DeleteButton.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadDocuments()
        {
            try
            {
                _allDocuments = DocumentLibraryRepository.GetAll();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Localization.T("Documents.Error.LoadFailed"), ex.Message),
                    Localization.T("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string CurrentCategoryFilter()
        {
            if (FilterImportantRadio.IsChecked == true) return "Important";
            if (FilterFreeRadio.IsChecked == true) return "Free";
            if (FilterOtherRadio.IsChecked == true) return "Other";
            return ""; // All
        }

        private void ApplyFilter()
        {
            var category = CurrentCategoryFilter();
            var filtered = string.IsNullOrEmpty(category)
                ? _allDocuments
                : _allDocuments.Where(d => string.Equals(d.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();

            filtered = ApplySort(filtered);

            DocumentsListBox.ItemsSource = filtered.Select(ToDisplayItem).ToList();
            NoDocumentsText.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            _selected = null;
            UpdateActionButtons();
        }

        private List<DocumentLibraryItem> ApplySort(List<DocumentLibraryItem> items) => _sortMode switch
        {
            "NameDesc" => items.OrderByDescending(d => d.DisplayName, StringComparer.OrdinalIgnoreCase).ToList(),
            "DateNewest" => items.OrderByDescending(d => d.UploadedDate).ToList(),
            "DateOldest" => items.OrderBy(d => d.UploadedDate).ToList(),
            "SizeLargest" => items.OrderByDescending(d => d.FileSizeBytes ?? 0).ToList(),
            "SizeSmallest" => items.OrderBy(d => d.FileSizeBytes ?? 0).ToList(),
            "Type" => items.OrderBy(d => d.Extension, StringComparer.OrdinalIgnoreCase)
                            .ThenBy(d => d.DisplayName, StringComparer.OrdinalIgnoreCase).ToList(),
            _ => items.OrderBy(d => d.DisplayName, StringComparer.OrdinalIgnoreCase).ToList() // "NameAsc" and any unrecognized value
        };

        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            if (SortButton.ContextMenu != null)
            {
                SortButton.ContextMenu.PlacementTarget = SortButton;
                SortButton.ContextMenu.IsOpen = true;
            }
        }

        private void SortMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem item || item.Tag is not string mode) return;
            _sortMode = mode;
            UpdateSortMenuChecks();
            ApplyFilter();
        }

        /// <summary>Keeps the sort menu acting like a single-select radio group (only the
        /// active sort mode shows a checkmark) even though MenuItem has no built-in grouping
        /// the way RadioButton does.</summary>
        private void UpdateSortMenuChecks()
        {
            SortNameAscItem.IsChecked = _sortMode == "NameAsc";
            SortNameDescItem.IsChecked = _sortMode == "NameDesc";
            SortDateNewestItem.IsChecked = _sortMode == "DateNewest";
            SortDateOldestItem.IsChecked = _sortMode == "DateOldest";
            SortSizeLargestItem.IsChecked = _sortMode == "SizeLargest";
            SortSizeSmallestItem.IsChecked = _sortMode == "SizeSmallest";
            SortTypeItem.IsChecked = _sortMode == "Type";
        }

        private DocumentDisplayItem ToDisplayItem(DocumentLibraryItem d) => new()
        {
            Source = d,
            DisplayName = d.DisplayName,
            IconGlyph = IconForExtension(d.Extension),
            IconSize = _iconSize,
            IconFontSize = _iconSize * 0.42
        };

        /// <summary>Maps a file extension to a representative glyph — grouped by the kind of
        /// file it is (document, spreadsheet, image, audio, video, code, archive, etc.) so the
        /// icon grid gives a quick visual read of what's in a folder full of mixed file types.</summary>
        private static string IconForExtension(string ext) => ext switch
        {
            // Documents
            ".pdf" => "📕",
            ".doc" or ".docx" or ".rtf" or ".odt" => "📝",
            ".txt" or ".md" or ".log" or ".rst" => "📃",

            // Spreadsheets
            ".xls" or ".xlsx" or ".xlsm" or ".csv" or ".ods" or ".tsv" => "📊",

            // Presentations
            ".ppt" or ".pptx" or ".odp" or ".key" => "📽️",

            // Images
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".tif" or ".tiff" or ".svg" or ".heic" => "🖼️",

            // Audio
            ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" or ".wma" or ".m4a" => "🎵",

            // Video
            ".mp4" or ".mov" or ".avi" or ".mkv" or ".wmv" or ".flv" or ".webm" or ".m4v" => "🎬",

            // Archives / disk images
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" or ".iso" => "🗜️",

            // Code / data
            ".cs" or ".js" or ".ts" or ".py" or ".java" or ".cpp" or ".c" or ".h" or ".php" or ".rb" or ".go" or ".sql" => "💻",
            ".json" or ".xml" or ".yaml" or ".yml" => "🗂️",
            ".html" or ".htm" or ".css" => "🌐",

            // Fonts, e-books, executables
            ".ttf" or ".otf" or ".woff" or ".woff2" => "🔤",
            ".epub" or ".mobi" => "📚",
            ".exe" or ".msi" => "⚙️",

            _ => "📄"
        };

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            if (DocumentsListBox == null) return; // still constructing the radio group
            ApplyFilter();
        }

        /// <summary>Icon size lives on each display item (not bound via ElementName into the
        /// DataTemplate, which sits in its own name scope) — so resizing just rebuilds the
        /// display list with the new size and re-binds it.</summary>
        private void IconSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _iconSize = e.NewValue;
            if (DocumentsListBox == null) return; // still constructing
            ApplyFilter();
        }

        private void DocumentsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selected = DocumentsListBox.SelectedItem as DocumentDisplayItem;
            UpdateActionButtons();
        }

        private void UpdateActionButtons()
        {
            bool hasSelection = _selected != null;
            bool canEdit = Session.CanEdit("Documents");
            OpenButton.IsEnabled = hasSelection;
            PrintButton.IsEnabled = hasSelection;
            RenameButton.IsEnabled = hasSelection && canEdit;
            DeleteButton.IsEnabled = hasSelection && canEdit;
        }

        private void ListBoxItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
            => Open_Click(sender, new RoutedEventArgs());

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            if (!Session.CanEdit("Documents")) return;

            var dialog = new OpenFileDialog
            {
                Title = Localization.T("Documents.Dialog.SelectFileTitle"),
                Filter = Localization.T("Documents.Dialog.Filter.AllFiles")
            };
            if (dialog.ShowDialog() != true) return;

            var suggestedName = Path.GetFileNameWithoutExtension(dialog.FileName);
            var extension = Path.GetExtension(dialog.FileName);

            var importWindow = new DocumentImportWindow(suggestedName, extension, "Important")
            {
                Owner = Window.GetWindow(this),
                Title = Localization.T("Documents.Dialog.ImportTitle")
            };
            if (importWindow.ShowDialog() != true) return;

            try
            {
                var storedPath = DocumentLibraryStorage.CopyIntoStorage(dialog.FileName, importWindow.ResultName + extension);
                var fileInfo = new FileInfo(storedPath);

                DocumentLibraryRepository.Add(new DocumentLibraryItem
                {
                    Category = importWindow.ResultCategory,
                    DisplayName = Path.GetFileName(storedPath), // may differ from requested name on a collision
                    OriginalFileName = Path.GetFileName(dialog.FileName),
                    StoredPath = storedPath,
                    FileSizeBytes = fileInfo.Length,
                    UploadedBy = Session.CurrentUser?.Username
                });

                LoadDocuments();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Localization.T("Documents.Error.ImportFailed"), ex.Message),
                    Localization.T("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            if (_selected == null) return;
            var path = _selected.Source.StoredPath;

            if (!File.Exists(path))
            {
                MessageBox.Show(Localization.T("Documents.Error.FileMissing"), Localization.T("Common.Error"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Localization.T("Documents.Error.OpenFailed"), ex.Message),
                    Localization.T("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            if (_selected == null) return;
            var path = _selected.Source.StoredPath;

            if (!File.Exists(path))
            {
                MessageBox.Show(Localization.T("Documents.Error.FileMissing"), Localization.T("Common.Error"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Relies on the file type having a registered "print" verb (Windows handles
                // this for the common office/PDF/image formats) — if none exists, the OS
                // throws, which we surface as a plain error rather than silently doing nothing.
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true, Verb = "print" });
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Localization.T("Documents.Error.PrintFailed"), ex.Message),
                    Localization.T("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            if (_selected == null || !Session.CanEdit("Documents")) return;

            var current = _selected.Source;
            var baseName = Path.GetFileNameWithoutExtension(current.DisplayName);
            var extension = Path.GetExtension(current.DisplayName);

            var renameWindow = new DocumentImportWindow(baseName, extension, current.Category)
            {
                Owner = Window.GetWindow(this),
                Title = Localization.T("Documents.Dialog.RenameTitle")
            };
            if (renameWindow.ShowDialog() != true) return;

            try
            {
                // Metadata only — the file on disk keeps its original stored name (same
                // convention as EmployeeDocumentRepository.Update), so nothing here can fail
                // due to the file being open elsewhere.
                DocumentLibraryRepository.UpdateMetadata(current.Id, renameWindow.ResultName + extension, renameWindow.ResultCategory);
                LoadDocuments();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Localization.T("Documents.Error.RenameFailed"), ex.Message),
                    Localization.T("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_selected == null || !Session.CanEdit("Documents")) return;

            if (MessageBox.Show(string.Format(Localization.T("Documents.Confirm.Delete"), _selected.DisplayName),
                Localization.T("Common.Confirm"), MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                DocumentLibraryRepository.Delete(_selected.Source.Id);
                LoadDocuments();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Localization.T("Documents.Error.DeleteFailed"), ex.Message),
                    Localization.T("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>View-only wrapper around a DocumentLibraryItem — carries the icon glyph and
        /// the current slider size so the ItemTemplate has plain properties to bind to.</summary>
        private class DocumentDisplayItem
        {
            public DocumentLibraryItem Source { get; set; } = null!;
            public string DisplayName { get; set; } = "";
            public string IconGlyph { get; set; } = "📄";
            public double IconSize { get; set; }
            public double IconFontSize { get; set; }
        }
    }
}
