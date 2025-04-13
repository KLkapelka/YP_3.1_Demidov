using System.Windows;
using Npgsql;
using System.Windows.Controls;
using System.Collections.Generic;

namespace ProjectManagementApp
{
    public partial class AddTaskWindow : Window
    {
        private int _projectId;

        // Конструктор с передачей projectId
        public AddTaskWindow(int projectId)
        {
            InitializeComponent();
            _projectId = projectId;
            LoadAssignedUsers(); // Загружаем пользователей при открытии окна
        }

        // Метод для загрузки пользователей, которые числятся в проекте
        private void LoadAssignedUsers()
        {
            var users = new List<string>();

            string query = @"
                SELECT u.full_name
                FROM ""Users"" u
                JOIN ""UserProjectRoles"" upr ON u.user_id = upr.user_id
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
                                users.Add(reader.GetString(0));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке пользователей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Очищаем ComboBox и добавляем пользователей
            cmbAssignedTo.Items.Clear();
            foreach (var user in users)
            {
                cmbAssignedTo.Items.Add(user);
            }
        }

        // Обработчик кнопки "Добавить"
        private void BtnAddTask_Click(object sender, RoutedEventArgs e)
        {
            string taskName = txtTaskName.Text;
            string taskDescription = txtTaskDescription.Text;
            string assignedTo = cmbAssignedTo.SelectedItem as string;
            string status = (cmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString();

            // Проверка на пустые поля
            if (string.IsNullOrWhiteSpace(taskName) || string.IsNullOrWhiteSpace(assignedTo) || string.IsNullOrWhiteSpace(status))
            {
                MessageBox.Show("Название задачи, ответственный и статус обязательны!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка выбора в ComboBox
            if (cmbAssignedTo.SelectedItem == null || cmbStatus.SelectedItem == null)
            {
                MessageBox.Show("Выберите ответственного и статус!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // SQL-запрос для добавления задачи
            string query = @"
                INSERT INTO ""Tasks"" (task_name, task_description, assigned_to, project_id, status)
                VALUES (@task_name, @task_description, @assigned_to, @project_id, @status)";

            try
            {
                using (var conn = new NpgsqlConnection(App.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("task_name", taskName);
                        cmd.Parameters.AddWithValue("task_description", string.IsNullOrEmpty(taskDescription) ? (object)DBNull.Value : taskDescription);
                        cmd.Parameters.AddWithValue("assigned_to", assignedTo);
                        cmd.Parameters.AddWithValue("project_id", _projectId);
                        cmd.Parameters.AddWithValue("status", status);
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Задача успешно добавлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close(); // Закрываем окно добавления задачи
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при добавлении задачи: {ex.Message}");
                MessageBox.Show($"Ошибка при добавлении задачи: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}