using System.Windows;
using HR_ERP.Data;
using HR_ERP.Helpers;
using HR_ERP.Views;
using Localization = HR_ERP.Helpers.Localization;

namespace HR_ERP
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Load the saved language before showing anything, so the login window itself
            // already opens in the right language/direction.
            try
            {
                var savedLanguage = SettingsRepository.Get("Language");
                Localization.SetLanguage(savedLanguage ?? "en");
            }
            catch
            {
                // Database not reachable yet (e.g. connection string not configured) — fall
                // back to English; the login window's own try/catch will surface the real
                // connection error once the person tries to sign in.
                Localization.SetLanguage("en");
            }

            var login = new LoginWindow();
            var signedIn = login.ShowDialog();

            if (signedIn != true || login.AuthenticatedUser == null)
            {
                Shutdown();
                return;
            }

            Session.SignIn(login.AuthenticatedUser);

            var main = new MainWindow();
            MainWindow = main;
            main.Show();
        }
    }
}
