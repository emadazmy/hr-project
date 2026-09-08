using System.Data;
using HR_ERP.Models;
using Microsoft.Data.SqlClient;

namespace HR_ERP.Data
{
    public static class RoleRepository
    {
        public static List<Role> GetAll()
        {
            var list = new List<Role>();
            var table = DatabaseHelper.ExecuteQuery("SELECT id, name, description FROM Roles ORDER BY name");
            foreach (DataRow row in table.Rows)
            {
                list.Add(new Role
                {
                    Id = (int)row["id"],
                    Name = row["name"].ToString() ?? "",
                    Description = row["description"] as string
                });
            }
            return list;
        }

        public static void Add(Role r)
        {
            var newId = DatabaseHelper.ExecuteScalar(
                "INSERT INTO Roles (name, description) OUTPUT INSERTED.id VALUES (@name, @desc)",
                new SqlParameter("@name", r.Name),
                new SqlParameter("@desc", (object?)r.Description ?? DBNull.Value));
            r.Id = Convert.ToInt32(newId);

            // A brand new role starts with no permission rows granted (view=0, edit=0) for
            // every module, so it shows up fully unchecked on the permissions matrix rather
            // than being silently missing rows.
            foreach (var p in PermissionRepository.GetAll())
            {
                DatabaseHelper.ExecuteNonQuery(
                    "INSERT INTO RolePermissions (role_id, permission_id, can_view, can_edit) VALUES (@role, @perm, 0, 0)",
                    new SqlParameter("@role", r.Id), new SqlParameter("@perm", p.Id));
            }
        }

        public static void Update(Role r)
        {
            DatabaseHelper.ExecuteNonQuery(
                "UPDATE Roles SET name = @name, description = @desc WHERE id = @id",
                new SqlParameter("@name", r.Name),
                new SqlParameter("@desc", (object?)r.Description ?? DBNull.Value),
                new SqlParameter("@id", r.Id));
        }

        public static void Delete(int id)
        {
            DatabaseHelper.ExecuteNonQuery("DELETE FROM Roles WHERE id = @id", new SqlParameter("@id", id));
        }

        /// <summary>Every module's view/edit flags for one role, in a fixed module order — the
        /// Settings screen's Roles &amp; Permissions grid binds directly to this.</summary>
        public static List<RolePermission> GetMatrixForRole(int roleId)
        {
            var list = new List<RolePermission>();
            var table = DatabaseHelper.ExecuteQuery(
                "SELECT p.id AS permission_id, p.module_key, p.display_name, " +
                "ISNULL(rp.can_view, 0) AS can_view, ISNULL(rp.can_edit, 0) AS can_edit " +
                "FROM Permissions p " +
                "LEFT JOIN RolePermissions rp ON rp.permission_id = p.id AND rp.role_id = @role " +
                "ORDER BY p.id",
                new SqlParameter("@role", roleId));

            foreach (DataRow row in table.Rows)
            {
                list.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = (int)row["permission_id"],
                    ModuleKey = row["module_key"].ToString() ?? "",
                    DisplayName = row["display_name"].ToString() ?? "",
                    CanView = (bool)row["can_view"],
                    CanEdit = (bool)row["can_edit"]
                });
            }
            return list;
        }

        /// <summary>Saves the whole matrix for one role in one call — upserts each module row
        /// (handles roles created before a new module/permission existed).</summary>
        public static void SaveMatrixForRole(int roleId, IEnumerable<RolePermission> rows)
        {
            foreach (var row in rows)
            {
                DatabaseHelper.ExecuteNonQuery(
                    "IF EXISTS (SELECT 1 FROM RolePermissions WHERE role_id = @role AND permission_id = @perm) " +
                    "  UPDATE RolePermissions SET can_view = @view, can_edit = @edit WHERE role_id = @role AND permission_id = @perm " +
                    "ELSE " +
                    "  INSERT INTO RolePermissions (role_id, permission_id, can_view, can_edit) VALUES (@role, @perm, @view, @edit)",
                    new SqlParameter("@role", roleId),
                    new SqlParameter("@perm", row.PermissionId),
                    new SqlParameter("@view", row.CanView),
                    new SqlParameter("@edit", row.CanEdit));
            }
        }

        /// <summary>Every module a given user's role can see, with the edit flag — loaded once
        /// at login and used to gate the left nav and each module's edit controls.</summary>
        public static Dictionary<string, (bool CanView, bool CanEdit)> GetPermissionsForRole(int roleId)
        {
            var dict = new Dictionary<string, (bool, bool)>(StringComparer.OrdinalIgnoreCase);
            var table = DatabaseHelper.ExecuteQuery(
                "SELECT p.module_key, rp.can_view, rp.can_edit FROM RolePermissions rp " +
                "JOIN Permissions p ON p.id = rp.permission_id WHERE rp.role_id = @role",
                new SqlParameter("@role", roleId));
            foreach (DataRow row in table.Rows)
                dict[row["module_key"].ToString() ?? ""] = ((bool)row["can_view"], (bool)row["can_edit"]);
            return dict;
        }
    }

    public static class PermissionRepository
    {
        public static List<Permission> GetAll()
        {
            var list = new List<Permission>();
            var table = DatabaseHelper.ExecuteQuery("SELECT id, module_key, display_name FROM Permissions ORDER BY id");
            foreach (DataRow row in table.Rows)
            {
                list.Add(new Permission
                {
                    Id = (int)row["id"],
                    ModuleKey = row["module_key"].ToString() ?? "",
                    DisplayName = row["display_name"].ToString() ?? ""
                });
            }
            return list;
        }
    }
}
