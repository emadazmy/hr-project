using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using HR_ERP.Data;
using HR_ERP.Models;

namespace HR_ERP.Views
{
    public partial class DocumentViewerWindow : Window
    {
        private readonly EmployeeDocument _document;
        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
        private static readonly string[] TextExtensions = { ".txt", ".csv", ".log", ".md", ".xml", ".json" };

        public DocumentViewerWindow(EmployeeDocument document)
        {
            InitializeComponent();
            _document = document;

            FileNameBox.Text = document.FileName;
            var options = EmployeeDocument.CategoryOptions;
            CategoryBox.ItemsSource = options;
            CategoryBox.SelectedItem = options.FirstOrDefault(o => o.Value == document.DocumentType) ?? options.FirstOrDefault();
            DescriptionBox.Text = document.Description;
            MetaText.Text = $"Uploaded {document.UploadedDate:g}";

            LoadPreview();
        }

        private void LoadPreview()
        {
            if (!File.Exists(_document.StoredPath))
            {
                ShowUnsupported("This file could not be found on disk (it may have been moved or deleted).");
                return;
            }

            var ext = Path.GetExtension(_document.StoredPath).ToLowerInvariant();

            try
            {
                if (ImageExtensions.Contains(ext))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // load fully so the file isn't left locked
                    bitmap.UriSource = new Uri(_document.StoredPath, UriKind.Absolute);
                    bitmap.EndInit();
                    ImageViewer.Source = bitmap;
                    ImageViewer.Visibility = Visibility.Visible;
                }
                else if (TextExtensions.Contains(ext))
                {
                    TextViewer.Text = File.ReadAllText(_document.StoredPath);
                    TextViewer.Visibility = Visibility.Visible;
                }
                else if (ext == ".pdf")
                {
                    // Rendered inline via the WPF WebBrowser control (hosts the system's installed
                    // PDF handler) — still inside this window, no separate application window opens.
                    PdfViewer.Navigate(new Uri(_document.StoredPath, UriKind.Absolute));
                    PdfViewer.Visibility = Visibility.Visible;
                }
                else
                {
                    ShowUnsupported(
                        $"No inline preview is available for '{ext}' files yet.\n\n" +
                        "Supported previews: images (.jpg, .png, .gif, .bmp), text (.txt, .csv, .log, .md, .xml, .json), and PDF.");
                }
            }
            catch (Exception ex)
            {
                ShowUnsupported("Could not preview this file: " + ex.Message);
            }
        }

        private void ShowUnsupported(string message)
        {
            UnsupportedText.Text = message;
            UnsupportedPanel.Visibility = Visibility.Visible;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FileNameBox.Text))
            {
                MessageBox.Show("File name cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _document.FileName = FileNameBox.Text.Trim();
                _document.DocumentType = (CategoryBox.SelectedItem as ComboOption)?.Value ?? "Other";
                _document.Description = DescriptionBox.Text.Trim();
                EmployeeDocumentRepository.Update(_document);
                MessageBox.Show("Saved.", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
