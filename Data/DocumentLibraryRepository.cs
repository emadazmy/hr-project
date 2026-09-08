using System.Data;
using HR_ERP.Models;
using Microsoft.Data.SqlClient;

namespace HR_ERP.Data
{
    public static class DocumentLibraryRepository
    {
        private const string BaseSelect =
            "SELECT id, category, display_name, original_file_name, stored_path, file_size, uploaded_date, uploaded_by FROM DocumentLibrary";

        public static List<DocumentLibraryItem> GetAll()
        {
            var list = new List<DocumentLibraryItem>();
            var table = DatabaseHelper.ExecuteQuery(BaseSelect + " ORDER BY category, display_name");
            foreach (DataRow row in table.Rows)
                list.Add(MapRow(row));
            return list;
        }

        public static DocumentLibraryItem? GetById(int id)
        {
            var table = DatabaseHelper.ExecuteQuery(BaseSelect + " WHERE id = @id", new SqlParameter("@id", id));
            return table.Rows.Count > 0 ? MapRow(table.Rows[0]) : null;
        }

        /// <summary>Inserts the DB record for a file already copied into storage
        /// (DocumentLibraryStorage.CopyIntoStorage) and sets item.Id to the new row.</summary>
        public static void Add(DocumentLibraryItem item)
        {
            var newId = DatabaseHelper.ExecuteScalar(
                "INSERT INTO DocumentLibrary (category, display_name, original_file_name, stored_path, file_size, uploaded_by) " +
                "OUTPUT INSERTED.id VALUES (@cat, @disp, @orig, @path, @size, @by)",
                new SqlParameter("@cat", item.Category),
                new SqlParameter("@disp", item.DisplayName),
                new SqlParameter("@orig", item.OriginalFileName),
                new SqlParameter("@path", item.StoredPath),
                new SqlParameter("@size", (object?)item.FileSizeBytes ?? DBNull.Value),
                new SqlParameter("@by", (object?)item.UploadedBy ?? DBNull.Value));
            item.Id = Convert.ToInt32(newId);
        }

        /// <summary>Updates the display name and/or category only — does not rename or move
        /// the underlying stored file, so StoredPath stays valid (same convention as
        /// EmployeeDocumentRepository.Update).</summary>
        public static void UpdateMetadata(int id, string newDisplayName, string newCategory)
        {
            DatabaseHelper.ExecuteNonQuery(
                "UPDATE DocumentLibrary SET display_name = @disp, category = @cat WHERE id = @id",
                new SqlParameter("@disp", newDisplayName),
                new SqlParameter("@cat", newCategory),
                new SqlParameter("@id", id));
        }

        /// <summary>Deletes the DB record and the underlying stored file copy.</summary>
        public static void Delete(int id)
        {
            var item = GetById(id);
            DatabaseHelper.ExecuteNonQuery("DELETE FROM DocumentLibrary WHERE id = @id", new SqlParameter("@id", id));
            if (item != null) DocumentLibraryStorage.DeleteFromStorage(item.StoredPath);
        }

        private static DocumentLibraryItem MapRow(DataRow row) => new()
        {
            Id = (int)row["id"],
            Category = row["category"].ToString() ?? "Other",
            DisplayName = row["display_name"].ToString() ?? "",
            OriginalFileName = row["original_file_name"].ToString() ?? "",
            StoredPath = row["stored_path"].ToString() ?? "",
            FileSizeBytes = row["file_size"] as long?,
            UploadedDate = (DateTime)row["uploaded_date"],
            UploadedBy = row["uploaded_by"] as string
        };
    }
}
