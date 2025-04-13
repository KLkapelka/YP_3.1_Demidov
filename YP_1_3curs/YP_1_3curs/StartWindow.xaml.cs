using System.Windows;

namespace ProjectManagementApp
{
    public partial class StartWindow : Window
    {
        public StartWindow()
        {
            InitializeComponent();
        }

        // Обработчик кнопки "Вход"
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            // Открываем окно входа
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close(); // Закрываем стартовое окно
        }

        // Обработчик кнопки "Регистрация"
        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            // Открываем окно регистрации
            var registerWindow = new MainWindow(); // Используем MainWindow для регистрации
            registerWindow.Show();
            this.Close(); // Закрываем стартовое окно
        }
    }
}