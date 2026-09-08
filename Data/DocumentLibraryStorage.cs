using System.IO;

namespace HR_ERP.Data
{
    /// <summary>
    /// Copies imported files into an "important documents" folder next to the application —
    /// the general-purpose Documents hub's storage, separate from the per-employee
    /// "EmployeeDocuments" folder (<see cref="DocumentStorage"/>). One flat folder for every
    /// category (Important/Free/Other); the category itself is only tracked in the
    /// DocumentLibrary table, not reflected in the folder structure.
    /// </summary>
    public static class DocumentLibraryStorage
    {
        public static string BaseFolder => Path.Combine(AppContext.BaseDirectory, "important documents");

        /// <summary>Copies sourceFilePath into the "important documents" folder under
        /// desiredFileName (renaming on collision), and returns the full path of the stored
        /// copy. This is the one point where the physical file is actually renamed — once
        /// imported, a later metadata rename (DocumentLibraryRepository.UpdateMetadata) only
        /// changes the display name in the database, not this file on disk.</summary>
        public static string CopyIntoStorage(string sourceFilePath, string desiredFileName)
        {
            Directory.CreateDirectory(BaseFolder);

            var fileName = SanitizeFileName(desiredFileName);
            var destPath = Path.Combine(BaseFolder, fileName);

            int counter = 1;
            while (File.Exists(destPath))
            {
                var name = Path.GetFileNameWithoutExtension(fileName);
                var ext = Path.GetExtension(fileName);
                destPath = Path.Combine(BaseFolder, $"{name} ({counter}){ext}");
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

        private static string SanitizeFileName(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            return value;
        }
    }
}
