using System.Windows;
using Npgsql;
using BCrypt.Net; // Добавьте библиотеку BCrypt.Net через NuGet
using System.Linq;

namespace ProjectManagementApp
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        // Обработчик кнопки "Войти"
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string phone = txtPhone.Text;
            string password = pwdPassword.Password;

            // Проверка на пустые поля
            if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Номер телефона и пароль обязательны!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка номера телефона
            if (!phone.All(char.IsDigit))
            {
                MessageBox.Show("Номер телефона должен содержать только цифры!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // SQL-запрос для получения хеша пароля и ID пользователя из базы данных
            string query = @"
                SELECT user_id, password
                FROM ""Users""
                WHERE phone_number = @phone_number";

            try
            {
                using (var conn = new NpgsqlConnection(App.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("phone_number", phone);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int userId = reader.GetInt32(0); // Получаем ID пользователя
                                string hashedPasswordFromDb = reader.GetString(1); // Получаем хеш пароля

                                // Сравнение хеша пароля из базы данных с хешем введённого пароля
                                if (BCrypt.Net.BCrypt.Verify(password, hashedPasswordFromDb))
                                {
                                    // Сохраняем ID текущего пользователя
                                    AppContext.CurrentUserId = userId;

                                    MessageBox.Show("Вход выполнен успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                                    // Открываем страницу с проектами
                                    var projectWindow = new ProjectWindow();
                                    projectWindow.Show();
                                    this.Close(); // Закрываем окно входа
                                }
                                else
                                {
                                    MessageBox.Show("Неверный номер телефона или пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                            else
                            {
                                MessageBox.Show("Пользователь с таким номером телефона не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при входе: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Кнопка "Назад"
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            // Открываем окно входа/регистрации
            var startWindow = new StartWindow();
            startWindow.Show();
            this.Close(); // Закрываем текущее окно
        }
        
        
    }
}