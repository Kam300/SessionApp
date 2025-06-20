using SessionApp1.Models;
using SessionApp1.Pages;
using SessionApp1.Services;
using System.Windows;
using System.Windows.Controls;

namespace SessionApp1.Views
{
    public partial class DirectorScreen : Page
    {
        private readonly User _currentUser;
        private readonly DatabaseService _databaseService;

        public DirectorScreen(User user)
        {
            InitializeComponent();
            _currentUser = user;
            _databaseService = new DatabaseService();
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

        private void StockReportButton_Click(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new MaterialStockReportPage(_databaseService, _currentUser));
        }

        private void MovementReportButton_Click(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new MaterialMovementReportPage(_databaseService, _currentUser));
        }

        private void OrderListButton_Click(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new OrderListPage(_databaseService, _currentUser));
        }
    }
}