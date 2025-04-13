using System.Windows;
using Npgsql;
using System.Collections.Generic;
using System.Linq;

namespace ProjectManagementApp
{
    public partial class AssignRoleWindow : Window
    {
        private int _projectId;
        private string _userFullName;
        private string _currentUserRole;

        public AssignRoleWindow(int projectId, string userFullName, string currentUserRole)
        {
            InitializeComponent();
            _projectId = projectId;
            _userFullName = userFullName;
            _currentUserRole = currentUserRole;
            LoadRoles();
        }

        private void LoadRoles()
        {
            var roles = new List<string>();

            string query = @"SELECT role_name FROM ""Roles""";

            try
            {
                using (var conn = new NpgsqlConnection(App.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                roles.Add(reader.GetString(0));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке ролей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Если текущий пользователь - администратор, убираем роли "Владелец" и "Администратор"
            if (_currentUserRole == "Администратор")
            {
                roles = roles.Where(r => r != "Владелец" && r != "Администратор").ToList();
            }

            cmbRoles.ItemsSource = roles;
        }

        private void BtnAssign_Click(object sender, RoutedEventArgs e)
        {
            string selectedRole = cmbRoles.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedRole))
            {
                MessageBox.Show("Выберите роль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Назначаем роль пользователю
            AssignRoleToUser(_userFullName, selectedRole);
            this.Close();
        }

        private void AssignRoleToUser(string userFullName, string roleName)
        {
            // Проверяем, есть ли у пользователя уже роль в проекте
            string checkQuery = @"
                SELECT COUNT(*)
                FROM ""UserProjectRoles"" upr
                JOIN ""Users"" u ON upr.user_id = u.user_id
                WHERE u.full_name = @full_name AND upr.project_id = @project_id";

            string insertQuery = @"
                INSERT INTO ""UserProjectRoles"" (user_id, project_id, role_id)
                SELECT u.user_id, @project_id, r.role_id
                FROM ""Users"" u, ""Roles"" r
                WHERE u.full_name = @full_name AND r.role_name = @role_name";

            string updateQuery = @"
                UPDATE ""UserProjectRoles""
                SET role_id = (SELECT role_id FROM ""Roles"" WHERE role_name = @role_name)
                WHERE user_id = (SELECT user_id FROM ""Users"" WHERE full_name = @full_name)
                AND project_id = @project_id";

            try
            {
                using (var conn = new NpgsqlConnection(App.ConnectionString))
                {
                    conn.Open();

                    // Проверяем, есть ли у пользователя уже роль в проекте
                    using (var checkCmd = new NpgsqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("full_name", userFullName);
                        checkCmd.Parameters.AddWithValue("project_id", _projectId);
                        long count = (long)checkCmd.ExecuteScalar();

                        if (count > 0)
                        {
                            // Если роль уже есть, обновляем её
                            using (var updateCmd = new NpgsqlCommand(updateQuery, conn))
                            {
                                updateCmd.Parameters.AddWithValue("full_name", userFullName);
                                updateCmd.Parameters.AddWithValue("project_id", _projectId);
                                updateCmd.Parameters.AddWithValue("role_name", roleName);
                                updateCmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // Если роли нет, добавляем новую запись
                            using (var insertCmd = new NpgsqlCommand(insertQuery, conn))
                            {
                                insertCmd.Parameters.AddWithValue("full_name", userFullName);
                                insertCmd.Parameters.AddWithValue("project_id", _projectId);
                                insertCmd.Parameters.AddWithValue("role_name", roleName);
                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
                MessageBox.Show("Роль успешно назначена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при назначении роли: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}