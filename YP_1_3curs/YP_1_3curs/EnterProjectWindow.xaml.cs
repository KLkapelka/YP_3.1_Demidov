using System.Windows;
using Npgsql;

namespace ProjectManagementApp
{
    public partial class EnterProjectWindow : Window
    {
        public EnterProjectWindow()
        {
            InitializeComponent();
        }

        // Обработчик кнопки "Войти в проект"
        private void BtnEnterProject_Click(object sender, RoutedEventArgs e)
        {
            string projectName = txtProjectName.Text;
            string projectPassword = pwdProjectPassword.Password;

            if (string.IsNullOrWhiteSpace(projectName) || string.IsNullOrWhiteSpace(projectPassword))
            {
                MessageBox.Show("Название проекта и пароль обязательны!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var conn = new NpgsqlConnection(App.ConnectionString))
                {
                    conn.Open();
                    string query = @"SELECT project_id FROM ""Projects"" WHERE project_name = @project_name AND project_password = @project_password";
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("project_name", projectName);
                        cmd.Parameters.AddWithValue("project_password", projectPassword);
                        var result = cmd.ExecuteScalar();

                        if (result != null)
                        {
                            int projectId = (int)result;

                            // Проверяем, существует ли пользователь
                            int currentUserId = GetCurrentUserId();
                            if (!UserExists(currentUserId))
                            {
                                MessageBox.Show("Пользователь не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            // Проверяем, существует ли проект
                            if (!ProjectExists(projectId))
                            {
                                MessageBox.Show("Проект не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            // Добавляем пользователя в проект
                            AddUserToProject(projectId);

                            MessageBox.Show("Вход в проект выполнен успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                            var projectManagementWindow = new ProjectManagementWindow(projectId);
                            projectManagementWindow.Show();
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Неверное название проекта или пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при входе в проект: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

       // Метод для добавления пользователя в проект
        private void AddUserToProject(int projectId)
        {
            int currentUserId = GetCurrentUserId(); // Получаем ID текущего пользователя

            // Проверяем, есть ли у пользователя уже роль в проекте
            string checkQuery = @"
                SELECT COUNT(*) 
                FROM ""UserProjectRoles"" 
                WHERE user_id = @user_id AND project_id = @project_id";

            try
            {
                using (var conn = new NpgsqlConnection(App.ConnectionString))
                {
                    conn.Open();

                    // Проверяем, есть ли у пользователя роль в проекте
                    using (var checkCmd = new NpgsqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("user_id", currentUserId);
                        checkCmd.Parameters.AddWithValue("project_id", projectId);
                        long count = (long)checkCmd.ExecuteScalar();

                        // Если у пользователя нет роли в проекте, добавляем его с ролью "Работник"
                        if (count == 0)
                        {
                            // Получаем role_id для роли "Работник"
                            int workerRoleId = GetRoleId("Работник");
                            if (workerRoleId == 0)
                            {
                                MessageBox.Show("Роль 'Работник' не найдена!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            // SQL-запрос для добавления пользователя в проект с ролью "Работник"
                            string insertQuery = @"
                                INSERT INTO ""UserProjectRoles"" (user_id, project_id, role_id)
                                VALUES (@user_id, @project_id, @role_id)";

                            using (var insertCmd = new NpgsqlCommand(insertQuery, conn))
                            {
                                insertCmd.Parameters.AddWithValue("user_id", currentUserId);
                                insertCmd.Parameters.AddWithValue("project_id", projectId);
                                insertCmd.Parameters.AddWithValue("role_id", workerRoleId);
                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении пользователя в проект: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    return result != null ? (int)result : 0; // Возвращаем 0, если роль не найдена
                }
            }
        }

        // Метод для получения текущего пользователя
        private int GetCurrentUserId()
        {
            // Пример: возвращаем ID текущего пользователя из глобальной переменной
            return AppContext.CurrentUserId; // Замените на реальный механизм
        }

        // Кнопка "Назад"
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            // Открываем окно выбора (создать проект/войти в проект)
            var projectWindow = new ProjectWindow();
            projectWindow.Show();
            this.Close(); // Закрываем текущее окно
        }
        
        // Метод для проверки существования пользователя
        private bool UserExists(int userId)
        {
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
        
        
    }
}