using System.Data;
using Microsoft.Data.SqlClient;

namespace HR_ERP.Data
{
    /// <summary>
    /// Central place to configure the SQL Server connection string.
    /// Update ServerName below to match your local SQL Server instance
    /// (e.g. "localhost", ".\\SQLEXPRESS", "(localdb)\\MSSQLLocalDB").
    /// </summary>
    public static class DatabaseHelper
    {
        // ---- EDIT THIS to match your environment ----
        private const string ServerName = "DESKTOP-PULSE";
        private const string DatabaseName = "HR_ERP";
        // If you use SQL auth instead of Windows auth, set UseIntegratedSecurity = false
        // and fill in SqlUser / SqlPassword.
        private const bool UseIntegratedSecurity = true;
        private const string SqlUser = "sa";
        private const string SqlPassword = "";

        public static string ConnectionString
        {
            get
            {
                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = ServerName,
                    InitialCatalog = DatabaseName,
                    TrustServerCertificate = true
                };

                if (UseIntegratedSecurity)
                {
                    builder.IntegratedSecurity = true;
                }
                else
                {
                    builder.UserID = SqlUser;
                    builder.Password = SqlPassword;
                }

                return builder.ConnectionString;
            }
        }

        public static SqlConnection GetConnection() => new SqlConnection(ConnectionString);

        public static DataTable ExecuteQuery(string sql, params SqlParameter[] parameters)
        {
            var table = new DataTable();
            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            if (parameters is { Length: > 0 })
                cmd.Parameters.AddRange(parameters);

            conn.Open();
            using var adapter = new SqlDataAdapter(cmd);
            adapter.Fill(table);
            return table;
        }

        public static int ExecuteNonQuery(string sql, params SqlParameter[] parameters)
        {
            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            if (parameters is { Length: > 0 })
                cmd.Parameters.AddRange(parameters);

            conn.Open();
            return cmd.ExecuteNonQuery();
        }

        public static object? ExecuteScalar(string sql, params SqlParameter[] parameters)
        {
            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            if (parameters is { Length: > 0 })
                cmd.Parameters.AddRange(parameters);

            conn.Open();
            return cmd.ExecuteScalar();
        }
    }
}
