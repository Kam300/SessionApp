using SessionApp1.Services;
using System.Windows;
using System.Windows.Controls;

namespace SessionApp1.Views
{
    public partial class RegisterPage : Page
    {
        private readonly DatabaseService _databaseService;

        public RegisterPage()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessage.Text = "";
            SuccessMessage.Text = "";

            if (string.IsNullOrWhiteSpace(FullNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(LoginTextBox.Text) ||
                string.IsNullOrWhiteSpace(PasswordBox.Password) ||
                string.IsNullOrWhiteSpace(ConfirmPasswordBox.Password))
            {
                ErrorMessage.Text = "Заполните все поля";
                return;
            }

            if (PasswordBox.Password != ConfirmPasswordBox.Password)
            {
                ErrorMessage.Text = "Пароли не совпадают";
                return;
            }

            if (PasswordBox.Password.Length < 6)
            {
                ErrorMessage.Text = "Пароль должен содержать минимум 6 символов";
                return;
            }

            try
            {
                var success = await _databaseService.RegisterUserAsync(
                    FullNameTextBox.Text.Trim(),
                    LoginTextBox.Text.Trim(),
                    PasswordBox.Password);

                if (success)
                {
                    SuccessMessage.Text = "Регистрация успешна! Теперь вы можете войти в систему.";
                    ClearFields();
                }
                else
                {
                    ErrorMessage.Text = "Пользователь с таким логином уже существует";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage.Text = $"Ошибка регистрации: {ex.Message}";
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.NavigateToPage(new LoginPage());
        }

        private void ClearFields()
        {
            FullNameTextBox.Text = "";
            LoginTextBox.Text = "";
            PasswordBox.Password = "";
            ConfirmPasswordBox.Password = "";
        }
    }
}
