using System.Windows;
using Npgsql;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;

namespace ProjectManagementApp
{
    public partial class EditTaskWindow : Window
    {
        private int _taskId;
        private int _projectId;

        public EditTaskWindow(int taskId)
        {
            InitializeComponent();
            _taskId = taskId;
            LoadTaskData();
        }

        private void LoadTaskData()
        {
            string query = @"
                SELECT task_name, task_description, assigned_to, status, project_id
                FROM ""Tasks""
                WHERE task_id = @task_id";

            try
            {
                using (var conn = new NpgsqlConnection(App.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("task_id", _taskId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtTaskName.Text = reader.GetString(0);
                                txtTaskDescription.Text = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                                _projectId = reader.GetInt32(4); // Сохраняем project_id

                                // Загружаем список пользователей в проекте
                                LoadAssignedUsers();

                                // Устанавливаем выбранного ответственного
                                cmbAssignedTo.SelectedItem = cmbAssignedTo.Items.Cast<string>()
                                    .FirstOrDefault(item => item == reader.GetString(2));

                                // Устанавливаем статус задачи
                                cmbStatus.SelectedItem = cmbStatus.Items.Cast<ComboBoxItem>()
                                    .FirstOrDefault(item => item.Content.ToString() == reader.GetString(3));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных задачи: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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

        private void BtnSaveTask_Click(object sender, RoutedEventArgs e)
        {
            string taskName = txtTaskName.Text;
            string taskDescription = txtTaskDescription.Text;
            string assignedTo = cmbAssignedTo.SelectedItem as string;
            string status = (cmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString();

            if (string.IsNullOrWhiteSpace(taskName) || string.IsNullOrWhiteSpace(assignedTo) || string.IsNullOrWhiteSpace(status))
            {
                MessageBox.Show("Название задачи, ответственный и статус обязательны!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string query = @"
                UPDATE ""Tasks""
                SET task_name = @task_name,
                    task_description = @task_description,
                    assigned_to = @assigned_to,
                    status = @status
                WHERE task_id = @task_id";

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
                        cmd.Parameters.AddWithValue("status", status);
                        cmd.Parameters.AddWithValue("task_id", _taskId);
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Задача успешно обновлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении задачи: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}