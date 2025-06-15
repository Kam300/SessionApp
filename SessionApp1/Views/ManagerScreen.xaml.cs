using SessionApp1.Models;
using System.Windows;
using System.Windows.Controls;

namespace SessionApp1.Views
{
    public partial class ManagerScreen : Page
    {
        private readonly User _currentUser;

        public ManagerScreen(User user)
        {
            InitializeComponent();
            _currentUser = user;
            WelcomeText.Text = $"Добро пожаловать, {_currentUser.FullName}!";

            // По умолчанию показываем список изделий
            ContentFrame.Navigate(new ProductsListPage());
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.NavigateToPage(new LoginPage());
        }

        private void ProductsButton_Click(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new ProductsListPage());
        }
    }
}