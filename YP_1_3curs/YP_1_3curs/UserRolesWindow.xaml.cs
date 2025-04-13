using System.Windows;
using Npgsql;
using System.Collections.Generic;

namespace ProjectManagementApp
{
    public partial class UserRolesWindow : Window
    {
        private int _projectId;
        private string _currentUserRole;

        public UserRolesWindow(int projectId)
        {
            InitializeComponent();
            _projectId = projectId;

            // Получаем роль текущего пользователя
            _currentUserRole = GetCurrentUserRole(projectId);

            // Настраиваем видимость кнопки "Назначить роль"
            btnAssignRole.Visibility = _currentUserRole == "Работник" ? Visibility.Collapsed : Visibility.Visible;

            LoadUsersAndRoles();
        }

        private void LoadUsersAndRoles()
        {
            var usersAndRoles = new List<UserRoleViewModel>();

            string query = @"
                SELECT u.full_name, r.role_name
                FROM ""Users"" u
                JOIN ""UserProjectRoles"" upr ON u.user_id = upr.user_id
                JOIN ""Roles"" r ON upr.role_id = r.role_id
                WHERE upr.project_id = @project_id";

            try
            {
                using (var conn = new NpgsqlConnection(App.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("project_id", _projectId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                usersAndRoles.Add(new UserRoleViewModel
                                {
                                    FullName = reader.GetString(0),
                                    RoleName = reader.GetString(1)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке пользователей и ролей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            lstUsers.ItemsSource = usersAndRoles;
        }

        private void BtnAssignRole_Click(object sender, RoutedEventArgs e)
        {
            var selectedUser = lstUsers.SelectedItem as UserRoleViewModel;
            if (selectedUser == null)
            {
                MessageBox.Show("Выберите пользователя для назначения роли!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Открываем окно для назначения роли
            var assignRoleWindow = new AssignRoleWindow(_projectId, selectedUser.FullName, _currentUserRole);
            assignRoleWindow.ShowDialog();
            LoadUsersAndRoles(); // Обновляем список после назначения роли
        }

        // Метод для получения роли текущего пользователя
        private string GetCurrentUserRole(int projectId)
        {
            int currentUserId = AppContext.CurrentUserId;

            string query = @"
                SELECT r.role_name
                FROM ""UserProjectRoles"" upr
                JOIN ""Roles"" r ON upr.role_id = r.role_id
                WHERE upr.user_id = @user_id AND upr.project_id = @project_id";

            try
            {
                using (var conn = new NpgsqlConnection(App.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("user_id", currentUserId);
                        cmd.Parameters.AddWithValue("project_id", projectId);
                        var result = cmd.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении роли пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
    }

    public class UserRoleViewModel
    {
        public string FullName { get; set; }
        public string RoleName { get; set; }
    }
}