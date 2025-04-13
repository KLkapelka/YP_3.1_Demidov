using System.Windows;

namespace ProjectManagementApp
{
    public partial class App : Application
    {
        // Строка подключения к базе данных
        public static string ConnectionString { get; } = 
            "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=QWEasd123!@#123";
    }
}