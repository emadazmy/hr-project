using System.Data;
using HR_ERP.Models;
using Microsoft.Data.SqlClient;

namespace HR_ERP.Data
{
    public static class DepartmentRepository
    {
        public static List<Department> GetAll()
        {
            var list = new List<Department>();
            var table = DatabaseHelper.ExecuteQuery("SELECT id, code, name, empl_code, type, [date], parent_code FROM department ORDER BY name");
            foreach (DataRow row in table.Rows)
            {
                list.Add(new Department
                {
                    Id = (int)row["id"],
                    Code = row["code"].ToString() ?? "",
                    Name = row["name"].ToString() ?? "",
                    EmplCode = row["empl_code"] as string,
                    Type = row["type"] as string,
                    Date = row["date"] as DateTime?,
                    ParentCode = row["parent_code"] as string
                });
            }
            return list;
        }

        public static void Add(Department d)
        {
            DatabaseHelper.ExecuteNonQuery(
                "INSERT INTO department (code, name, empl_code, type, [date], parent_code) VALUES (@code, @name, @empl, @type, @date, @parent)",
                new SqlParameter("@code", d.Code),
                new SqlParameter("@name", d.Name),
                new SqlParameter("@empl", (object?)d.EmplCode ?? DBNull.Value),
                new SqlParameter("@type", (object?)d.Type ?? DBNull.Value),
                new SqlParameter("@date", (object?)d.Date ?? DBNull.Value),
                new SqlParameter("@parent", (object?)d.ParentCode ?? DBNull.Value));
        }

        public static void Update(Department d)
        {
            DatabaseHelper.ExecuteNonQuery(
                "UPDATE department SET code = @code, name = @name, empl_code = @empl, type = @type, [date] = @date, parent_code = @parent WHERE id = @id",
                new SqlParameter("@code", d.Code),
                new SqlParameter("@name", d.Name),
                new SqlParameter("@empl", (object?)d.EmplCode ?? DBNull.Value),
                new SqlParameter("@type", (object?)d.Type ?? DBNull.Value),
                new SqlParameter("@date", (object?)d.Date ?? DBNull.Value),
                new SqlParameter("@parent", (object?)d.ParentCode ?? DBNull.Value),
                new SqlParameter("@id", d.Id));
        }

        public static void Delete(int id)
        {
            DatabaseHelper.ExecuteNonQuery("DELETE FROM department WHERE id = @id", new SqlParameter("@id", id));
        }
    }

    public static class JobPositionRepository
    {
        public static List<JobPosition> GetAll()
        {
            var list = new List<JobPosition>();
            var table = DatabaseHelper.ExecuteQuery("SELECT ID, Code, Title, Depart, JobDescription FROM JobPositions ORDER BY Title");
            foreach (DataRow row in table.Rows)
            {
                list.Add(new JobPosition
                {
                    Id = (int)row["ID"],
                    Code = row["Code"] as string,
                    Title = row["Title"].ToString() ?? "",
                    Depart = row["Depart"] as string,
                    JobDescription = row["JobDescription"] as string
                });
            }
            return list;
        }

        public static List<JobPosition> GetByDepartment(string departmentName)
        {
            var list = new List<JobPosition>();
            var table = DatabaseHelper.ExecuteQuery(
                "SELECT ID, Code, Title, Depart, JobDescription FROM JobPositions WHERE Depart = @dep ORDER BY Title",
                new SqlParameter("@dep", departmentName));
            foreach (DataRow row in table.Rows)
            {
                list.Add(new JobPosition
                {
                    Id = (int)row["ID"],
                    Code = row["Code"] as string,
                    Title = row["Title"].ToString() ?? "",
                    Depart = row["Depart"] as string,
                    JobDescription = row["JobDescription"] as string
                });
            }
            return list;
        }

        public static void Add(JobPosition p)
        {
            DatabaseHelper.ExecuteNonQuery(
                "INSERT INTO JobPositions (Code, Title, Depart, JobDescription) VALUES (@code, @title, @dep, @desc)",
                new SqlParameter("@code", (object?)p.Code ?? DBNull.Value),
                new SqlParameter("@title", p.Title),
                new SqlParameter("@dep", (object?)p.Depart ?? DBNull.Value),
                new SqlParameter("@desc", (object?)p.JobDescription ?? DBNull.Value));
        }

        public static void Update(JobPosition p)
        {
            DatabaseHelper.ExecuteNonQuery(
                "UPDATE JobPositions SET Code = @code, Title = @title, Depart = @dep, JobDescription = @desc WHERE ID = @id",
                new SqlParameter("@code", (object?)p.Code ?? DBNull.Value),
                new SqlParameter("@title", p.Title),
                new SqlParameter("@dep", (object?)p.Depart ?? DBNull.Value),
                new SqlParameter("@desc", (object?)p.JobDescription ?? DBNull.Value),
                new SqlParameter("@id", p.Id));
        }

        public static void Delete(int id)
        {
            DatabaseHelper.ExecuteNonQuery("DELETE FROM JobPositions WHERE ID = @id", new SqlParameter("@id", id));
        }
    }
}
