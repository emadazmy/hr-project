using System.Windows;
using System.Windows.Controls;
using HR_ERP.Data;
using HR_ERP.Helpers;
using HR_ERP.Models;
using Localization = HR_ERP.Helpers.Localization;

namespace HR_ERP.Views
{
    public partial class SettingsView : UserControl
    {
        private List<Role> _roles = new();
        private List<Employee> _employees = new();
        private User? _selectedUser;
        private Role? _selectedRole;
        private List<RolePermission> _currentMatrix = new();

        public SettingsView()
        {
            InitializeComponent();
            ApplyLocalization();
            LoadRoles();
            LoadEmployees();
            LoadUsers();
            SetLanguageRadios();
        }

        private void ApplyLocalization()
        {
            TitleText.Text = Localization.T("Settings.Title");
            UsersTab.Header = Localization.T("Settings.Tab.Users");
            RolesTab.Header = Localization.T("Settings.Tab.Roles");
            LanguageTab.Header = Localization.T("Settings.Tab.Language");

            UserUsernameLabel.Text = Localization.T("Settings.Users.Username");
            UserFullNameLabel.Text = Localization.T("Settings.Users.FullName");
            UserRoleLabel.Text = Localization.T("Settings.Users.Role");
            UserEmployeeLabel.Text = Localization.T("Settings.Users.Employee");
            UserActiveBox.Content = Localization.T("Settings.Users.Active");
            UserPasswordLabel.Text = Localization.T("Settings.Users.Password");
            UserNewButton.Content = Localization.T("Settings.Users.New");
            UserSaveButton.Content = Localization.T("Settings.Users.Save");
            UserDeleteButton.Content = Localization.T("Settings.Users.Delete");
            UserResetPasswordButton.Content = Localization.T("Settings.Users.ResetPassword");

            RoleNameLabel.Text = Localization.T("Settings.Roles.Name");
            RoleDescriptionLabel.Text = Localization.T("Settings.Roles.Description");
            RoleNewButton.Content = Localization.T("Settings.Roles.New");
            RoleSaveButton.Content = Localization.T("Settings.Roles.Save");
            RoleDeleteButton.Content = Localization.T("Settings.Roles.Delete");
            SavePermissionsButton.Content = Localization.T("Settings.Roles.SavePermissions");

            LanguagePromptText.Text = Localization.T("Settings.Language.Prompt");
            EnglishRadio.Content = Localization.T("Settings.Language.English");
            ArabicRadio.Content = Localization.T("Settings.Language.Arabic");
            RestartNoteText.Text = Localization.T("Settings.Language.RestartNote");
            ApplyLanguageButton.Content = Localization.T("Settings.Language.Apply");
        }

        // ---------------------------------------------------------- Users

        private void LoadUsers()
        {
            try
            {
                UsersGrid.ItemsSource = UserRepository.GetAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load users: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadEmployees()
        {
            try
            {
                _employees = EmployeeRepository.GetAll();
                UserEmployeeBox.ItemsSource = _employees;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load employees: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRoles()
        {
            try
            {
                _roles = RoleRepository.GetAll();
                UserRoleBox.ItemsSource = _roles;
                RolesGrid.ItemsSource = _roles;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load roles: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UsersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UsersGrid.SelectedItem is not User u) return;
            _selectedUser = u;
            UserUsernameBox.Text = u.Username;
            UserFullNameBox.Text = u.FullName;
            UserRoleBox.SelectedItem = _roles.FirstOrDefault(r => r.Id == u.RoleId);
            UserEmployeeBox.SelectedItem = _employees.FirstOrDefault(emp => emp.Code == u.EmployeeCode);
            UserActiveBox.IsChecked = u.IsActive;
            UserPasswordBox.Password = "";
            PasswordPanel.Visibility = Visibility.Visible; // still shown, but blank = "leave unchanged" for existing users
        }

        private void UserNew_Click(object sender, RoutedEventArgs e)
        {
            _selectedUser = null;
            UsersGrid.SelectedItem = null;
            UserUsernameBox.Text = "";
            UserFullNameBox.Text = "";
            UserRoleBox.SelectedItem = null;
            UserEmployeeBox.SelectedItem = null;
            UserActiveBox.IsChecked = true;
            UserPasswordBox.Password = "";
        }

        private void UserSave_Click(object sender, RoutedEventArgs e)
        {
            var username = UserUsernameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(UserFullNameBox.Text) || UserRoleBox.SelectedItem is not Role role)
            {
                MessageBox.Show("Username, full name and role are required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (UserRepository.UsernameExists(username, _selectedUser?.Id ?? 0))
            {
                MessageBox.Show("That username is already taken.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var user = new User
            {
                Id = _selectedUser?.Id ?? 0,
                Username = username,
                FullName = UserFullNameBox.Text.Trim(),
                EmployeeCode = (UserEmployeeBox.SelectedItem as Employee)?.Code,
                RoleId = role.Id,
                IsActive = UserActiveBox.IsChecked == true
            };

            try
            {
                if (_selectedUser == null)
                {
                    if (string.IsNullOrWhiteSpace(UserPasswordBox.Password))
                    {
                        MessageBox.Show("A password is required for a new user.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    UserRepository.Add(user, UserPasswordBox.Password);
                }
                else
                {
                    UserRepository.Update(user);
                    if (!string.IsNullOrWhiteSpace(UserPasswordBox.Password))
                        UserRepository.ResetPassword(user.Id, UserPasswordBox.Password);
                }

                LoadUsers();
                UserNew_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UserResetPassword_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null)
            {
                MessageBox.Show("Select a user first.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(UserPasswordBox.Password))
            {
                MessageBox.Show("Type the new password in the Password field, then click Reset Password.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                UserRepository.ResetPassword(_selectedUser.Id, UserPasswordBox.Password);
                UserPasswordBox.Password = "";
                MessageBox.Show("Password updated.", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Reset failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UserDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null) return;
            if (_selectedUser.Id == Session.CurrentUser?.Id)
            {
                MessageBox.Show("You can't delete the account you're currently signed in as.", "Not allowed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBox.Show($"Delete user '{_selectedUser.Username}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            try
            {
                UserRepository.Delete(_selectedUser.Id);
                LoadUsers();
                UserNew_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Delete failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------------------------------------------------------- Roles & Permissions

        private void RolesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RolesGrid.SelectedItem is not Role r) return;
            _selectedRole = r;
            RoleNameBox.Text = r.Name;
            RoleDescriptionBox.Text = r.Description;
            LoadMatrix(r.Id);
        }

        private void LoadMatrix(int roleId)
        {
            try
            {
                _currentMatrix = RoleRepository.GetMatrixForRole(roleId);
                PermissionsGrid.ItemsSource = _currentMatrix;
                MatrixHintText.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load permissions: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RoleNew_Click(object sender, RoutedEventArgs e)
        {
            _selectedRole = null;
            RolesGrid.SelectedItem = null;
            RoleNameBox.Text = "";
            RoleDescriptionBox.Text = "";
            PermissionsGrid.ItemsSource = null;
            MatrixHintText.Visibility = Visibility.Visible;
        }

        private void RoleSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(RoleNameBox.Text))
            {
                MessageBox.Show("Role name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var role = new Role
            {
                Id = _selectedRole?.Id ?? 0,
                Name = RoleNameBox.Text.Trim(),
                Description = RoleDescriptionBox.Text.Trim()
            };

            try
            {
                if (_selectedRole == null) RoleRepository.Add(role);
                else RoleRepository.Update(role);

                LoadRoles();
                RoleNew_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RoleDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRole == null) return;
            if (Session.CurrentUser?.RoleId == _selectedRole.Id)
            {
                MessageBox.Show("You can't delete the role your current account uses.", "Not allowed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBox.Show($"Delete role '{_selectedRole.Name}'? Any user assigned to it must be reassigned first.",
                "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            try
            {
                RoleRepository.Delete(_selectedRole.Id);
                LoadRoles();
                RoleNew_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Delete failed (it may still be assigned to a user): " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SavePermissions_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRole == null)
            {
                MessageBox.Show("Select a role first.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Commit any in-progress checkbox edit before reading the values back.
            PermissionsGrid.CommitEdit(DataGridEditingUnit.Row, true);

            try
            {
                RoleRepository.SaveMatrixForRole(_selectedRole.Id, _currentMatrix);
                MessageBox.Show("Permissions saved.", "Done", MessageBoxButton.OK, MessageBoxImage.Information);

                // If the signed-in user's own role was just changed, refresh their session
                // permissions immediately so the nav reflects it without a re-login.
                if (Session.CurrentUser?.RoleId == _selectedRole.Id)
                    Session.SignIn(Session.CurrentUser);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------------------------------------------------------- Language

        private void SetLanguageRadios()
        {
            EnglishRadio.Checked -= LanguageRadio_Checked;
            ArabicRadio.Checked -= LanguageRadio_Checked;
            EnglishRadio.IsChecked = Localization.CurrentLanguage == "en";
            ArabicRadio.IsChecked = Localization.CurrentLanguage == "ar";
            EnglishRadio.Checked += LanguageRadio_Checked;
            ArabicRadio.Checked += LanguageRadio_Checked;
        }

        private void LanguageRadio_Checked(object sender, RoutedEventArgs e)
        {
            // Just selecting a radio button doesn't apply anything yet — Apply does that,
            // so switching languages is a deliberate action, not a side effect of browsing.
        }

        private void ApplyLanguage_Click(object sender, RoutedEventArgs e)
        {
            var languageCode = ArabicRadio.IsChecked == true ? "ar" : "en";

            try
            {
                SettingsRepository.Set("Language", languageCode);
                Localization.SetLanguage(languageCode); // fires LanguageChanged -> MainWindow flips FlowDirection + re-localizes nav
                ApplyLocalization(); // re-localize this screen's own labels immediately too
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not save the language setting: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
