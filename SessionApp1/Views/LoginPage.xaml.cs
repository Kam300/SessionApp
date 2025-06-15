using SessionApp1.Models;
using SessionApp1.Services;
using System.Windows;
using System.Windows.Controls;

namespace SessionApp1.Views
{
    public partial class LoginPage : Page
    {
        private readonly DatabaseService _databaseService;

        public LoginPage()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessage.Text = "";

            if (string.IsNullOrWhiteSpace(LoginTextBox.Text) ||
                string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                ErrorMessage.Text = "Заполните все поля";
                return;
            }

            try
            {
                var user = await _databaseService.AuthenticateUserAsync(
                    LoginTextBox.Text.Trim(),
                    PasswordBox.Password);

                if (user != null)
                {
                    NavigateToUserScreen(user);
                }
                else
                {
                    ErrorMessage.Text = "Неверный логин или пароль";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage.Text = $"Ошибка подключения: {ex.Message}";
            }
        }

        private void NavigateToUserScreen(User user)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;

            switch (user.RoleName.ToLower())
            {
                case "заказчик":
                    mainWindow.NavigateToPage(new CustomerScreen(user));
                    break;
                case "менеджер":
                    mainWindow.NavigateToPage(new ManagerScreen(user));
                    break;
                case "кладовщик":
                    mainWindow.NavigateToPage(new WarehouseScreen(user));
                    break;
                case "дирекция":
                    mainWindow.NavigateToPage(new DirectorScreen(user));
                    break;
                default:
                    ErrorMessage.Text = "Неизвестная роль пользователя";
                    break;
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.NavigateToPage(new RegisterPage());
        }
    }
}
