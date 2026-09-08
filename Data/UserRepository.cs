using System.Data;
using HR_ERP.Models;
using Microsoft.Data.SqlClient;

namespace HR_ERP.Data
{
    public static class UserRepository
    {
        private const string BaseSelect =
            "SELECT u.id, u.username, u.password_hash, u.password_salt, u.full_name, u.employee_code, " +
            "u.role_id, r.name AS role_name, u.is_active, u.created_date, u.last_login " +
            "FROM Users u JOIN Roles r ON r.id = u.role_id";

        public static List<User> GetAll()
        {
            var list = new List<User>();
            var table = DatabaseHelper.ExecuteQuery(BaseSelect + " ORDER BY u.username");
            foreach (DataRow row in table.Rows)
                list.Add(Map(row));
            return list;
        }

        /// <summary>Looks up an active user by username and verifies the password.
        /// Returns null on any failure (unknown username, wrong password, disabled account)
        /// without revealing which — same message either way at the call site.</summary>
        public static User? Authenticate(string username, string password)
        {
            var table = DatabaseHelper.ExecuteQuery(
                BaseSelect + " WHERE u.username = @u AND u.is_active = 1",
                new SqlParameter("@u", username));

            if (table.Rows.Count == 0) return null;

            var user = Map(table.Rows[0]);
            if (!AuthHelper.VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
                return null;

            DatabaseHelper.ExecuteNonQuery("UPDATE Users SET last_login = GETDATE() WHERE id = @id",
                new SqlParameter("@id", user.Id));

            return user;
        }

        public static void Add(User u, string plainPassword)
        {
            var (hash, salt) = AuthHelper.HashPassword(plainPassword);
            DatabaseHelper.ExecuteNonQuery(
                "INSERT INTO Users (username, password_hash, password_salt, full_name, employee_code, role_id, is_active) " +
                "VALUES (@user, @hash, @salt, @name, @emp, @role, @active)",
                new SqlParameter("@user", u.Username),
                new SqlParameter("@hash", hash),
                new SqlParameter("@salt", salt),
                new SqlParameter("@name", u.FullName),
                new SqlParameter("@emp", (object?)u.EmployeeCode ?? DBNull.Value),
                new SqlParameter("@role", u.RoleId),
                new SqlParameter("@active", u.IsActive));
        }

        /// <summary>Updates profile fields only (username, name, employee link, role, active flag) — not the password.</summary>
        public static void Update(User u)
        {
            DatabaseHelper.ExecuteNonQuery(
                "UPDATE Users SET username = @user, full_name = @name, employee_code = @emp, role_id = @role, is_active = @active WHERE id = @id",
                new SqlParameter("@user", u.Username),
                new SqlParameter("@name", u.FullName),
                new SqlParameter("@emp", (object?)u.EmployeeCode ?? DBNull.Value),
                new SqlParameter("@role", u.RoleId),
                new SqlParameter("@active", u.IsActive),
                new SqlParameter("@id", u.Id));
        }

        public static void ResetPassword(int userId, string newPlainPassword)
        {
            var (hash, salt) = AuthHelper.HashPassword(newPlainPassword);
            DatabaseHelper.ExecuteNonQuery(
                "UPDATE Users SET password_hash = @hash, password_salt = @salt WHERE id = @id",
                new SqlParameter("@hash", hash),
                new SqlParameter("@salt", salt),
                new SqlParameter("@id", userId));
        }

        public static void Delete(int id)
        {
            DatabaseHelper.ExecuteNonQuery("DELETE FROM Users WHERE id = @id", new SqlParameter("@id", id));
        }

        public static bool UsernameExists(string username, int excludeId = 0)
        {
            var result = DatabaseHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM Users WHERE username = @u AND id <> @id",
                new SqlParameter("@u", username), new SqlParameter("@id", excludeId));
            return Convert.ToInt32(result) > 0;
        }

        private static User Map(DataRow row) => new User
        {
            Id = (int)row["id"],
            Username = row["username"].ToString() ?? "",
            PasswordHash = row["password_hash"].ToString() ?? "",
            PasswordSalt = row["password_salt"].ToString() ?? "",
            FullName = row["full_name"].ToString() ?? "",
            EmployeeCode = row["employee_code"] as string,
            RoleId = (int)row["role_id"],
            RoleName = row["role_name"] as string,
            IsActive = (bool)row["is_active"],
            CreatedDate = (DateTime)row["created_date"],
            LastLogin = row["last_login"] as DateTime?
        };
    }
}
