using System.Windows;
using Npgsql;
using System.Linq;
using System.Collections.Generic;

namespace ProjectManagementApp
{
    public partial class ProjectManagementWindow : Window
    {
        private int _currentProjectId; // Добавляем поле для хранения projectId

        // Конструктор с передачей projectId
        public ProjectManagementWindow(int projectId)
        {
            InitializeComponent();
            _currentProjectId = projectId; // Сохраняем projectId

            // Получаем роль текущего пользователя
            string userRole = GetCurrentUserRole(projectId);

            // Настраиваем права доступа в зависимости от роли
            SetAccessBasedOnRole(userRole);

            LoadTasks(); // Загружаем задачи проекта при открытии окна
        }

        private void LoadTasks()
        {
            var tasks = new List<TaskViewModel>();

            string query = @"
                SELECT task_id, task_name, status, assigned_to
                FROM ""Tasks""
                WHERE project_id = @project_id";

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
                                tasks.Add(new TaskViewModel
                                {
                                    TaskId = reader.GetInt32(0),
                                    TaskName = reader.GetString(1),
                                    Status = reader.GetString(2),
                                    AssignedTo = reader.GetString(3)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке задач: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            lstTasks.ItemsSource = tasks;
        }

        // Обработчик кнопки "Добавить задачу"
        private void BtnAddTask_Click(object sender, RoutedEventArgs e)
        {
            // Открываем окно добавления задачи, передавая projectId
            var addTaskWindow = new AddTaskWindow(_currentProjectId);
            addTaskWindow.ShowDialog();
            LoadTasks(); // Обновляем список задач после добавления
        }

        private void BtnEditTask_Click(object sender, RoutedEventArgs e)
        {
            var selectedTask = lstTasks.SelectedItem as TaskViewModel;
            if (selectedTask == null)
            {
                MessageBox.Show("Выберите задачу для редактирования!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Получаем роль текущего пользователя
            string userRole = GetCurrentUserRole(_currentProjectId);

            // Если пользователь - работник, открываем окно только для редактирования статуса
            if (userRole == "Работник")
            {
                var editTaskStatusWindow = new EditTaskStatusWindow(selectedTask.TaskId);
                editTaskStatusWindow.ShowDialog();
            }
            else
            {
                // Для других ролей открываем полное окно редактирования
                var editTaskWindow = new EditTaskWindow(selectedTask.TaskId);
                editTaskWindow.ShowDialog();
            }

            LoadTasks(); // Обновляем список задач после редактирования
        }

        private void BtnDeleteTask_Click(object sender, RoutedEventArgs e)
        {
            var selectedTask = lstTasks.SelectedItem as TaskViewModel;
            if (selectedTask == null)
            {
                MessageBox.Show("Выберите задачу для удаления!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Вы уверены, что хотите удалить эту задачу?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                string query = @"
                    DELETE FROM ""Tasks""
                    WHERE task_id = @task_id";

                try
                {
                    using (var conn = new NpgsqlConnection(App.ConnectionString))
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("task_id", selectedTask.TaskId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    MessageBox.Show("Задача успешно удалена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadTasks();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении задачи: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Кнопка назад
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            // Открываем окно выбора (создать проект/войти в проект)
            var projectWindow = new ProjectWindow();
            projectWindow.Show();
            this.Close(); // Закрываем текущее окно
        }

        // Пользователи и роли
        private void BtnViewUsers_Click(object sender, RoutedEventArgs e)
        {
            var userRolesWindow = new UserRolesWindow(_currentProjectId);
            userRolesWindow.ShowDialog();
        }

        // Обработчик кнопки "Прогресс задач"
        private void BtnTaskProgress_Click(object sender, RoutedEventArgs e)
        {
            var taskProgressWindow = new TaskProgressWindow(_currentProjectId);
            taskProgressWindow.ShowDialog();
        }

        // Метод для получения роли текущего пользователя в проекте
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

        // Метод для настройки прав доступа в зависимости от роли
        private void SetAccessBasedOnRole(string role)
        {
            switch (role)
            {
                case "Владелец":
                    // Владелец имеет доступ ко всем кнопкам
                    btnAddTask.IsEnabled = true;
                    btnEditTask.IsEnabled = true;
                    btnDeleteTask.IsEnabled = true;
                    btnViewUsers.IsEnabled = true;
                    btnTaskProgress.IsEnabled = true;
                    break;

                case "Администратор":
                    // Администратор не может назначать роли Администратор и Владелец, но может всё остальное
                    btnAddTask.IsEnabled = true;
                    btnEditTask.IsEnabled = true;
                    btnDeleteTask.IsEnabled = true;
                    btnViewUsers.IsEnabled = true; // Может просматривать пользователей и роли
                    btnTaskProgress.IsEnabled = true;
                    break;

                case "Работник":
                    // Работник может только редактировать статус задачи и просматривать прогресс
                    btnAddTask.IsEnabled = false; // Не может добавлять задачи
                    btnEditTask.IsEnabled = true; // Может редактировать задачу (только статус)
                    btnDeleteTask.IsEnabled = false; // Не может удалять задачи
                    btnViewUsers.IsEnabled = true; // Может просматривать пользователей и роли
                    btnTaskProgress.IsEnabled = true; // Может просматривать прогресс
                    break;

                default:
                    // Если роль не определена, ограничиваем доступ
                    btnAddTask.IsEnabled = false;
                    btnEditTask.IsEnabled = false;
                    btnDeleteTask.IsEnabled = false;
                    btnViewUsers.IsEnabled = false;
                    btnTaskProgress.IsEnabled = false;
                    break;
            }
        }
    }

    public class TaskViewModel
    {
        public int TaskId { get; set; }
        public string TaskName { get; set; }
        public string Status { get; set; }
        public string AssignedTo { get; set; }
    }
}