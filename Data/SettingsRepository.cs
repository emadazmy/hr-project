using Microsoft.Data.SqlClient;

namespace HR_ERP.Data
{
    /// <summary>Simple key/value store backing app-wide settings — currently just Language,
    /// with room to grow (e.g. a default currency, a company name) without a schema change.</summary>
    public static class SettingsRepository
    {
        public static string? Get(string key)
        {
            var result = DatabaseHelper.ExecuteScalar(
                "SELECT [value] FROM AppSettings WHERE [key] = @k",
                new SqlParameter("@k", key));
            return result as string;
        }

        public static void Set(string key, string value)
        {
            DatabaseHelper.ExecuteNonQuery(
                "IF EXISTS (SELECT 1 FROM AppSettings WHERE [key] = @k) " +
                "  UPDATE AppSettings SET [value] = @v WHERE [key] = @k " +
                "ELSE " +
                "  INSERT INTO AppSettings ([key], [value]) VALUES (@k, @v)",
                new SqlParameter("@k", key), new SqlParameter("@v", value));
        }
    }
}
