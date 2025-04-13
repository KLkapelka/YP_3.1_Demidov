using System.Windows;

namespace ProjectManagementApp
{
    public partial class ProjectWindow : Window
    {
        public ProjectWindow()
        {
            InitializeComponent();
        }

        // Обработчик кнопки "Создать проект"
        private void BtnCreateProject_Click(object sender, RoutedEventArgs e)
        {
            // Открываем окно создания проекта
            var createProjectWindow = new CreateProjectWindow();
            createProjectWindow.Show();
            this.Close(); // Закрываем текущее окно
        }

        // Обработчик кнопки "Войти в проект"
        private void BtnEnterProject_Click(object sender, RoutedEventArgs e)
        {
            // Открываем окно входа в проект
            var enterProjectWindow = new EnterProjectWindow();
            enterProjectWindow.Show();
            this.Close(); // Закрываем текущее окно
        }
        
        // кнопка назад
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            // Открываем окно входа/регистрации
            var startWindow = new StartWindow();
            startWindow.Show();
            this.Close(); // Закрываем текущее окно
        }
    }
}