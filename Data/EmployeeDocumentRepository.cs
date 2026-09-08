using System.Data;
using HR_ERP.Models;
using Microsoft.Data.SqlClient;

namespace HR_ERP.Data
{
    public static class EmployeeDocumentRepository
    {
        private const string BaseSelect = "SELECT id, employee_code, document_type, file_name, stored_path, description, uploaded_date FROM EmployeeDocuments";

        public static List<EmployeeDocument> GetByEmployee(string employeeCode)
        {
            var list = new List<EmployeeDocument>();
            var table = DatabaseHelper.ExecuteQuery(
                BaseSelect + " WHERE employee_code = @code ORDER BY document_type, uploaded_date DESC",
                new SqlParameter("@code", employeeCode));

            foreach (DataRow row in table.Rows)
                list.Add(MapRow(row));
            return list;
        }

        private static EmployeeDocument MapRow(DataRow row) => new()
        {
            Id = (int)row["id"],
            EmployeeCode = row["employee_code"].ToString() ?? "",
            DocumentType = row["document_type"].ToString() ?? "Other",
            FileName = row["file_name"].ToString() ?? "",
            StoredPath = row["stored_path"].ToString() ?? "",
            Description = row["description"] as string,
            UploadedDate = (DateTime)row["uploaded_date"]
        };

        public static void Add(EmployeeDocument doc)
        {
            DatabaseHelper.ExecuteNonQuery(
                "INSERT INTO EmployeeDocuments (employee_code, document_type, file_name, stored_path, description, uploaded_date) VALUES (@code, @type, @file, @path, @desc, GETDATE())",
                new SqlParameter("@code", doc.EmployeeCode),
                new SqlParameter("@type", doc.DocumentType),
                new SqlParameter("@file", doc.FileName),
                new SqlParameter("@path", doc.StoredPath),
                new SqlParameter("@desc", (object?)doc.Description ?? DBNull.Value));
        }

        /// <summary>Updates the display file name, category, and/or description (metadata only —
        /// does not rename or move the underlying stored file, so StoredPath stays valid).</summary>
        public static void Update(EmployeeDocument doc)
        {
            DatabaseHelper.ExecuteNonQuery(
                "UPDATE EmployeeDocuments SET document_type = @type, file_name = @file, description = @desc WHERE id = @id",
                new SqlParameter("@type", doc.DocumentType),
                new SqlParameter("@file", doc.FileName),
                new SqlParameter("@desc", (object?)doc.Description ?? DBNull.Value),
                new SqlParameter("@id", doc.Id));
        }

        public static EmployeeDocument? GetById(int id)
        {
            var table = DatabaseHelper.ExecuteQuery(BaseSelect + " WHERE id = @id", new SqlParameter("@id", id));
            return table.Rows.Count > 0 ? MapRow(table.Rows[0]) : null;
        }

        /// <summary>Deletes the DB record and the underlying stored file copy.</summary>
        public static void Delete(int id)
        {
            var doc = GetById(id);
            DatabaseHelper.ExecuteNonQuery("DELETE FROM EmployeeDocuments WHERE id = @id", new SqlParameter("@id", id));
            if (doc != null) DocumentStorage.DeleteFromStorage(doc.StoredPath);
        }
    }
}
