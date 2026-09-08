using System.IO;

namespace HR_ERP.Data
{
    /// <summary>
    /// Copies imported employee documents into an "EmployeeDocuments" folder next to the
    /// application, so they live inside the app's own storage rather than staying wherever
    /// the user originally picked them from (and the original file can be moved/deleted
    /// afterward without breaking anything).
    /// </summary>
    public static class DocumentStorage
    {
        public static string BaseFolder => Path.Combine(AppContext.BaseDirectory, "EmployeeDocuments");

        /// <summary>Copies sourceFilePath into EmployeeDocuments\{employeeCode}\, renaming on
        /// collision, and returns the full path of the stored copy.</summary>
        public static string CopyIntoStorage(string employeeCode, string sourceFilePath)
        {
            var employeeFolder = Path.Combine(BaseFolder, SanitizeForPath(employeeCode));
            Directory.CreateDirectory(employeeFolder);

            var fileName = Path.GetFileName(sourceFilePath);
            var destPath = Path.Combine(employeeFolder, fileName);

            // Avoid overwriting an existing file with the same name.
            int counter = 1;
            while (File.Exists(destPath))
            {
                var name = Path.GetFileNameWithoutExtension(fileName);
                var ext = Path.GetExtension(fileName);
                destPath = Path.Combine(employeeFolder, $"{name} ({counter}){ext}");
                counter++;
            }

            File.Copy(sourceFilePath, destPath);
            return destPath;
        }

        public static void DeleteFromStorage(string storedPath)
        {
            try
            {
                if (File.Exists(storedPath)) File.Delete(storedPath);
            }
            catch
            {
                // If the file is locked or already gone, we still let the DB record be removed —
                // the caller isn't blocked by a stray file on disk.
            }
        }

        private static string SanitizeForPath(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            return value;
        }
    }
}
