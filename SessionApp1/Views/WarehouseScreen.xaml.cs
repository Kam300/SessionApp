using SessionApp1.Models;
using System.Windows;
using System.Windows.Controls;

namespace SessionApp1.Views
{
    public partial class WarehouseScreen : Page
    {
        private readonly User _currentUser;

        public WarehouseScreen(User user)
        {
            InitializeComponent();
            _currentUser = user;
            WelcomeText.Text = $"Добро пожаловать, {_currentUser.FullName}!";

            // По умолчанию показываем список тканей
            ContentFrame.Navigate(new FabricsListPage());
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.NavigateToPage(new LoginPage());
        }

        private void FabricsButton_Click(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new FabricsListPage());
        }

        private void FittingsButton_Click(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new FittingsListPage());
        }
        private void MaterialStockButton_Click(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new MaterialStockPage());
        }

        private void MaterialReceiptButton_Click(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new MaterialReceiptPage());
        }

    }
}