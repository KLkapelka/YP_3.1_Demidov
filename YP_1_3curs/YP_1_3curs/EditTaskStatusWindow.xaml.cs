using System.Windows;
using Npgsql;
using System.Linq;

namespace ProjectManagementApp
{
    public partial class EditTaskStatusWindow : Window
    {
        private int _taskId;

        public EditTaskStatusWindow(int taskId)
        {
            InitializeComponent();
            _taskId = taskId;
            LoadTaskStatus();
        }

        private void LoadTaskStatus()
        {
            string query = @"SELECT status FROM ""Tasks"" WHERE task_id = @task_id";

            try
            {
                using (var conn = new NpgsqlConnection(App.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("task_id", _taskId);
                        var result = cmd.ExecuteScalar();

                        if (result != null)
                        {
                            // Устанавливаем выбранный статус в ComboBox
                            string currentStatus = result.ToString();
                            cmbStatus.SelectedItem = cmbStatus.Items.Cast<string>()
                                .FirstOrDefault(item => item == currentStatus);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке статуса задачи: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSaveStatus_Click(object sender, RoutedEventArgs e)
        {
            string status = cmbStatus.SelectedItem as string;

            if (string.IsNullOrWhiteSpace(status))
            {
                MessageBox.Show("Статус задачи обязателен!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string query = @"UPDATE ""Tasks"" SET status = @status WHERE task_id = @task_id";

            try
            {
                using (var conn = new NpgsqlConnection(App.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("status", status);
                        cmd.Parameters.AddWithValue("task_id", _taskId);
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Статус задачи успешно обновлён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении статуса задачи: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}