using System.Windows;
using Npgsql;
using System.Windows.Controls;

namespace ProjectManagementApp
{
    public partial class CreateProjectWindow : Window
    {
        public CreateProjectWindow()
        {
            InitializeComponent();
        }

        // Обработчик кнопки "Создать проект"
        private void BtnCreateProject_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, что пользователь авторизован
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
            {
                MessageBox.Show("Пользователь не авторизован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string projectName = txtProjectName.Text;
            string projectPassword = pwdProjectPassword.Password;
            string description = txtDescription.Text;
            string status = (cmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString();

            // Проверка на пустые поля
            if (string.IsNullOrWhiteSpace(projectName) || projectName.Length > 255)
            {
                MessageBox.Show("Название проекта должно содержать от 1 до 255 символов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(projectPassword) || projectPassword.Length > 100)
            {
                MessageBox.Show("Пароль проекта должен содержать от 1 до 100 символов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(status))
            {
                MessageBox.Show("Статус проекта обязателен!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // SQL-запрос для создания проекта
            string query = @"
                INSERT INTO ""Projects"" (project_name, project_password, description, status)
                VALUES (@project_name, @project_password, @description, @status)
                RETURNING project_id"; // Возвращаем project_id созданного проекта

            try
            {
                using (var conn = new NpgsqlConnection(App.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("project_name", projectName);
                        cmd.Parameters.AddWithValue("project_password", projectPassword);
                        cmd.Parameters.AddWithValue("description", string.IsNullOrEmpty(description) ? DBNull.Value : (object)description);
                        cmd.Parameters.AddWithValue("status", status);

                        // Выполняем запрос и получаем project_id созданного проекта
                        int projectId = (int)cmd.ExecuteScalar();

                        MessageBox.Show("Проект успешно создан!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Назначаем текущего пользователя владельцем проекта
                        if (!AssignOwnerRole(projectId))
                        {
                            MessageBox.Show("Не удалось назначить роль владельца. Пожалуйста, проверьте настройки ролей.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        // Открываем окно управления проектом, передавая projectId
                        var projectManagementWindow = new ProjectManagementWindow(projectId);
                        projectManagementWindow.Show();

                        // Закрываем текущее окно создания проекта
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании проекта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Метод для назначения роли владельца
        private bool AssignOwnerRole(int projectId)
        {
            int currentUserId = GetCurrentUserId(); // Получаем ID текущего пользователя

            // Проверяем, что пользователь существует
            if (!UserExists(currentUserId))
            {
                MessageBox.Show("Пользователь не найден! Убедитесь, что вы авторизованы.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Проверяем, что проект существует
            if (!ProjectExists(projectId))
            {
                MessageBox.Show("Проект не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Получаем role_id для роли "Владелец"
            int ownerRoleId = GetRoleId("Владелец");
            if (ownerRoleId == 0)
            {
                MessageBox.Show("Роль 'Владелец' не найдена и не может быть создана!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // SQL-запрос для назначения роли владельца
            string assignOwnerQuery = @"
                INSERT INTO ""UserProjectRoles"" (user_id, project_id, role_id)
                VALUES (@user_id, @project_id, @role_id)";

            try
            {
                using (var conn = new NpgsqlConnection(App.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(assignOwnerQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("user_id", currentUserId);
                        cmd.Parameters.AddWithValue("project_id", projectId);
                        cmd.Parameters.AddWithValue("role_id", ownerRoleId);
                        cmd.ExecuteNonQuery();
                    }
                }
                return true; // Роль успешно назначена
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при назначении роли владельца: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // Метод для проверки существования пользователя
        private bool UserExists(int userId)
        {
            if (userId == 0)
            {
                return false; // Если ID пользователя равен 0, пользователь не существует
            }

            string query = @"SELECT COUNT(*) FROM ""Users"" WHERE user_id = @user_id";
            using (var conn = new NpgsqlConnection(App.ConnectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("user_id", userId);
                    long count = (long)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        // Метод для проверки существования проекта
        private bool ProjectExists(int projectId)
        {
            string query = @"SELECT COUNT(*) FROM ""Projects"" WHERE project_id = @project_id";
            using (var conn = new NpgsqlConnection(App.ConnectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("project_id", projectId);
                    long count = (long)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        // Метод для получения role_id по имени роли
        private int GetRoleId(string roleName)
        {
            string query = @"SELECT role_id FROM ""Roles"" WHERE role_name = @role_name";
            using (var conn = new NpgsqlConnection(App.ConnectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("role_name", roleName);
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        return (int)result;
                    }
                    else
                    {
                        // Если роль не найдена, создаём её
                        string insertQuery = @"INSERT INTO ""Roles"" (role_name) VALUES (@role_name) RETURNING role_id";
                        using (var insertCmd = new NpgsqlCommand(insertQuery, conn))
                        {
                            insertCmd.Parameters.AddWithValue("role_name", roleName);
                            return (int)insertCmd.ExecuteScalar();
                        }
                    }
                }
            }
        }
        
        // Метод для получения текущего пользователя
        private int GetCurrentUserId()
        {
            // Убедитесь, что AppContext.CurrentUserId инициализирован
            if (AppContext.CurrentUserId == 0)
            {
                MessageBox.Show("Текущий пользователь не авторизован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return 0; // Возвращаем 0, если пользователь не авторизован
            }
            return AppContext.CurrentUserId;
        }

        // Кнопка "Назад"
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            // Открываем окно выбора (создать проект/войти в проект)
            var projectWindow = new ProjectWindow();
            projectWindow.Show();
            this.Close(); // Закрываем текущее окно
        }
    }
}