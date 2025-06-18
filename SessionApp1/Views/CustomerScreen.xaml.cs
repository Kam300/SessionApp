using SessionApp1.Models;
using SessionApp1.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SessionApp1.Views
{
    public partial class CustomerScreen : Page
    {
        private User _currentUser;

        public CustomerScreen()
        {
            InitializeComponent();
        }

        public CustomerScreen(User currentUser) : this()
        {
            _currentUser = currentUser;
            InitializeForUser();
        }

        private void InitializeForUser()
        {
            if (_currentUser != null)
            {
                WelcomeText.Text = $"Добро пожаловать, {_currentUser.FullName}!";
            }
        }

        private void OpenCatalog_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null)
            {
                MessageBox.Show("Ошибка авторизации", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var catalogWindow = new Window
            {
                Title = "Каталог изделий",
                Content = new ProductCatalogPage(_currentUser),
                Width = 1000,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            catalogWindow.ShowDialog();
        }

        private async void MyOrders_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null)
            {
                MessageBox.Show("Ошибка авторизации", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var orderService = new OrderService();
                var orders = await orderService.GetOrdersByCustomerAsync(_currentUser.Id);

                var ordersInfo = $"У вас {orders.Count} заказов:\n\n";
                foreach (var order in orders.Take(10))
                {
                    ordersInfo += $"Заказ №{order.Id} от {order.OrderDate:dd.MM.yyyy} - {order.Status} - {order.TotalAmount:C}\n";
                }

                MessageBox.Show(ordersInfo, "Мои заказы", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.NavigateToPage(new LoginPage());
        }
        
    }
}
