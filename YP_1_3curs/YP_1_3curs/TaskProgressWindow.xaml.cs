using System.Windows;
using Npgsql;
using System;

namespace ProjectManagementApp
{
    public partial class TaskProgressWindow : Window
    {
        private int _currentProjectId;

        public TaskProgressWindow(int projectId)
        {
            InitializeComponent();
            _currentProjectId = projectId;
            LoadTaskProgress();
        }

        private void LoadTaskProgress()
        {
            int pendingCount = 0;
            int inProgressCount = 0;
            int completedCount = 0;

            string query = @"
                SELECT status, COUNT(*) as task_count
                FROM ""Tasks""
                WHERE project_id = @project_id
                GROUP BY status";

            try
            {
                using (var conn = new NpgsqlConnection(App.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("project_id", _currentProjectId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string status = reader.GetString(0);
                                int count = reader.GetInt32(1);

                                switch (status)
                                {
                                    case "В ожидании":
                                        pendingCount = count;
                                        break;
                                    case "В работе":
                                        inProgressCount = count;
                                        break;
                                    case "Завершена":
                                        completedCount = count;
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке прогресса задач: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Отображаем количество задач по статусам
            txtPending.Text = pendingCount.ToString();
            txtInProgress.Text = inProgressCount.ToString();
            txtCompleted.Text = completedCount.ToString();

            // Рассчитываем общий прогресс проекта
            int totalTasks = pendingCount + inProgressCount + completedCount;
            int progressPercentage = totalTasks == 0 ? 0 : (completedCount * 100) / totalTasks;

            // Отображаем прогресс
            progressBar.Value = progressPercentage;
            txtProgressPercentage.Text = $"{progressPercentage}%";
        }
    }
}