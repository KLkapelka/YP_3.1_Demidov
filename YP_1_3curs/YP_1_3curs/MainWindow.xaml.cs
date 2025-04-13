using System.Windows;
using Npgsql;
using System.Linq;

namespace ProjectManagementApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // Обработчик кнопки регистрации
        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            string fullName = txtFullName.Text;
            string phone = txtPhone.Text;
            string password = pwdPassword.Password;

            // Проверка на пустые поля
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("ФИО и пароль обязательны!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка длины пароля
            if (password.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать не менее 6 символов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка номера телефона
            if (!string.IsNullOrEmpty(phone) && !phone.All(char.IsDigit))
            {
                MessageBox.Show("Номер телефона должен содержать только цифры!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка уникальности номера телефона
            if (IsPhoneNumberExists(phone))
            {
                MessageBox.Show("Этот номер телефона уже был использован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Хеширование пароля
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            // SQL-запрос для регистрации пользователя
            string query = @"
                INSERT INTO ""Users"" (full_name, phone_number, password)
                VALUES (@full_name, @phone_number, @password)";

            // Выполнение запроса с параметрами
            try
            {
                using (var conn = new NpgsqlConnection(App.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("full_name", fullName);
                        cmd.Parameters.AddWithValue("phone_number", phone);
                        cmd.Parameters.AddWithValue("password", hashedPassword);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Авторизуем пользователя после регистрации
                AuthorizeUserAfterRegistration(phone);

                MessageBox.Show("Пользователь успешно зарегистрирован!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                // Открываем страницу с проектами
                var projectWindow = new ProjectWindow();
                projectWindow.Show();
                this.Close(); // Закрываем окно регистрации
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при регистрации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Метод для проверки уникальности номера телефона
        private bool IsPhoneNumberExists(string phone)
        {
            if (string.IsNullOrEmpty(phone))
                return false;

            string query = @"
                SELECT COUNT(*)
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
                        long count = (long)cmd.ExecuteScalar();
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при проверке номера телефона: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return true; // В случае ошибки считаем, что номер уже существует
            }
        }
        
        // кнопка назад
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            // Открываем окно входа/регистрации
            var startWindow = new StartWindow();
            startWindow.Show();
            this.Close(); // Закрываем текущее окно
        }
        
        // Метод для авторизации пользователя после регистрации
        private void AuthorizeUserAfterRegistration(string phone)
        {
            string query = @"
        SELECT user_id
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
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            int userId = (int)result;
                            AppContext.CurrentUserId = userId; // Сохраняем ID пользователя
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при авторизации после регистрации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // Метод для получения текущего пользователя
        private int GetCurrentUserId()
        {
            if (AppContext.CurrentUserId == 0)
            {
                MessageBox.Show("Текущий пользователь не авторизован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return AppContext.CurrentUserId;
        }
    }
}