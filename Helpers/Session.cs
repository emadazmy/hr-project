using HR_ERP.Data;
using HR_ERP.Models;

namespace HR_ERP.Helpers
{
    /// <summary>Holds the currently logged-in user and their role's module permissions for
    /// the lifetime of the app (single-user desktop session, so a static holder is enough —
    /// no need to thread a user object through every view/repository call).</summary>
    public static class Session
    {
        public static User? CurrentUser { get; private set; }
        private static Dictionary<string, (bool CanView, bool CanEdit)> _permissions = new();

        public static void SignIn(User user)
        {
            CurrentUser = user;
            _permissions = RoleRepository.GetPermissionsForRole(user.RoleId);
        }

        public static void SignOut()
        {
            CurrentUser = null;
            _permissions = new();
        }

        public static bool CanView(string moduleKey) =>
            _permissions.TryGetValue(moduleKey, out var p) && p.CanView;

        public static bool CanEdit(string moduleKey) =>
            _permissions.TryGetValue(moduleKey, out var p) && p.CanEdit;
    }
}
