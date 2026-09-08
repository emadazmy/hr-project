using System.Windows;
using System.Windows.Input;
using HR_ERP.Data;
using HR_ERP.Helpers;
using HR_ERP.Models;
using Localization = HR_ERP.Helpers.Localization;

namespace HR_ERP.Views
{
    public partial class LoginWindow : Window
    {
        public User? AuthenticatedUser { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();
            ApplyLocalization();
            Loaded += (_, _) => UsernameBox.Focus();
        }

        private void ApplyLocalization()
        {
            FlowDirection = Localization.FlowDirection;
            Title = Localization.T("Login.Title");
            TitleText.Text = Localization.T("AppTitle");
            SubtitleText.Text = Localization.T("AppSubtitle");
            UsernameLabel.Text = Localization.T("Login.Username");
            PasswordLabel.Text = Localization.T("Login.Password");
            SignInButton.Content = Localization.T("Login.SignIn");
        }

        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SignIn_Click(sender, e);
        }

        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowError(Localization.T("Login.Required"));
                return;
            }

            try
            {
                var user = UserRepository.Authenticate(username, password);
                if (user == null)
                {
                    ShowError(Localization.T("Login.InvalidCredentials"));
                    return;
                }

                AuthenticatedUser = user;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError("Could not reach the database: " + ex.Message);
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
