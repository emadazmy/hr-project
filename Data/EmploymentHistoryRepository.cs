using System.Data;
using HR_ERP.Models;
using Microsoft.Data.SqlClient;

namespace HR_ERP.Data
{
    public static class EmploymentHistoryRepository
    {
        private const string BaseSelect = @"
            SELECT id, employee_code, event_date, event_type, old_department, new_department,
                   old_position, new_position, old_salary, new_salary, performance_rating, notes, recorded_date
            FROM EmploymentHistory";

        public static List<EmploymentHistoryEntry> GetByEmployee(string employeeCode)
        {
            var list = new List<EmploymentHistoryEntry>();
            var table = DatabaseHelper.ExecuteQuery(
                BaseSelect + " WHERE employee_code = @code ORDER BY event_date DESC, id DESC",
                new SqlParameter("@code", employeeCode));

            foreach (DataRow row in table.Rows)
                list.Add(MapRow(row));
            return list;
        }

        private static EmploymentHistoryEntry MapRow(DataRow row) => new()
        {
            Id = (int)row["id"],
            EmployeeCode = row["employee_code"].ToString() ?? "",
            EventDate = (DateTime)row["event_date"],
            EventType = row["event_type"].ToString() ?? "Other",
            OldDepartment = row["old_department"] as string,
            NewDepartment = row["new_department"] as string,
            OldPosition = row["old_position"] as string,
            NewPosition = row["new_position"] as string,
            OldSalary = row["old_salary"] as decimal?,
            NewSalary = row["new_salary"] as decimal?,
            PerformanceRating = row["performance_rating"] as string,
            Notes = row["notes"] as string,
            RecordedDate = (DateTime)row["recorded_date"]
        };

        public static void Add(EmploymentHistoryEntry entry)
        {
            DatabaseHelper.ExecuteNonQuery(@"
                INSERT INTO EmploymentHistory (employee_code, event_date, event_type, old_department, new_department,
                    old_position, new_position, old_salary, new_salary, performance_rating, notes)
                VALUES (@code, @date, @type, @oldDept, @newDept, @oldPos, @newPos, @oldSalary, @newSalary, @rating, @notes)",
                new SqlParameter("@code", entry.EmployeeCode),
                new SqlParameter("@date", entry.EventDate),
                new SqlParameter("@type", entry.EventType),
                new SqlParameter("@oldDept", (object?)entry.OldDepartment ?? DBNull.Value),
                new SqlParameter("@newDept", (object?)entry.NewDepartment ?? DBNull.Value),
                new SqlParameter("@oldPos", (object?)entry.OldPosition ?? DBNull.Value),
                new SqlParameter("@newPos", (object?)entry.NewPosition ?? DBNull.Value),
                new SqlParameter("@oldSalary", (object?)entry.OldSalary ?? DBNull.Value),
                new SqlParameter("@newSalary", (object?)entry.NewSalary ?? DBNull.Value),
                new SqlParameter("@rating", (object?)entry.PerformanceRating ?? DBNull.Value),
                new SqlParameter("@notes", (object?)entry.Notes ?? DBNull.Value));
        }

        public static void Delete(int id)
        {
            DatabaseHelper.ExecuteNonQuery("DELETE FROM EmploymentHistory WHERE id = @id", new SqlParameter("@id", id));
        }
    }
}
