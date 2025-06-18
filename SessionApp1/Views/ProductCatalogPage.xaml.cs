using SessionApp1.Models;
using SessionApp1.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SessionApp1.Views
{
    public partial class ProductCatalogPage : Page
    {
        private List<ManufacturedGood> _products;
        private Order _currentOrder;
        private OrderService _orderService;
        private User _currentUser;

        public ProductCatalogPage(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _orderService = new OrderService();
            _currentOrder = new Order
            {
                CustomerUserId = currentUser.Id,
                Customer = currentUser.FullName,
                Status = "Формируется"
            };
            LoadProducts();
            
            // Добавляем обработчик двойного клика для открытия деталей изделия
            ProductsDataGrid.MouseDoubleClick += ProductsDataGrid_MouseDoubleClick;
        }

        private async void LoadProducts()
        {
            try
            {
                var dbService = new DatabaseService();
                _products = await dbService.GetManufacturedGoodsAsync();
                ProductsDataGrid.ItemsSource = _products;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки каталога: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddToOrder_Click(object sender, RoutedEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is ManufacturedGood selectedProduct)
            {
                if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity <= 0)
                {
                    MessageBox.Show("Укажите корректное количество (больше 0).", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var existingItem = _currentOrder.Items.FirstOrDefault(i => i.ProductArticle == selectedProduct.Article);
                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                    MessageBox.Show($"Изделие '{selectedProduct.Name}' добавлено в заказ.\nТеперь в заказе: {existingItem.Quantity} шт.",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _currentOrder.Items.Add(new OrderItem
                    {
                        ProductArticle = selectedProduct.Article,
                        ProductName = selectedProduct.Name,
                        Quantity = quantity,
                        Price = selectedProduct.Price
                    });
                    MessageBox.Show($"Изделие '{selectedProduct.Name}' добавлено в заказ: {quantity} шт.",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                QuantityTextBox.Text = "1";
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите изделие для добавления.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void GoToOrder_Click(object sender, RoutedEventArgs e)
        {
            if (_currentOrder.Items.Count == 0)
            {
                MessageBox.Show("Сначала добавьте изделия в заказ.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var orderWindow = new CreateOrderWindow(_currentOrder, _orderService);
            if (orderWindow.ShowDialog() == true)
            {
                _currentOrder = new Order
                {
                    CustomerUserId = _currentUser.Id,
                    Customer = _currentUser.FullName,
                    Status = "Формируется"
                };
            }
        }
        
        private void ProductsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is ManufacturedGood selectedProduct)
            {
                try
                {
                    // Открываем страницу с деталями изделия, передавая информацию о текущем пользователе
                    var detailPage = new ProductDetailPage(selectedProduct.Article, _currentUser);
                    
                    // Создаем новое окно для отображения деталей изделия
                    var detailWindow = new Window
                    {
                        Title = $"Детали изделия: {selectedProduct.Name}",
                        Content = detailPage,
                        Width = 900,
                        Height = 700,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen
                    };
                    
                    detailWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии деталей изделия: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
