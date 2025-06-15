using SessionApp1.Models;
using System.Windows;
using System.Windows.Controls;

namespace SessionApp1.Views
{
    public partial class CustomerScreen : Page
    {
        private readonly User _currentUser;

        public CustomerScreen(User user)
        {
            InitializeComponent();
            _currentUser = user;
            WelcomeText.Text = $"Добро пожаловать, {_currentUser.FullName}!";
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.NavigateToPage(new LoginPage());
        }
    }
}