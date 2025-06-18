using SessionApp1.Models;
using SessionApp1.Services;
using System;
using System.Linq;
using System.Windows;

namespace SessionApp1.Views
{
    public partial class CreateOrderWindow : Window
    {
        private Order _order;
        private OrderService _orderService;

        public CreateOrderWindow(Order order, OrderService orderService)
        {
            InitializeComponent();
            _order = order;
            _orderService = orderService;
            InitializeWindow();
        }

        private void InitializeWindow()
        {
            CustomerNameTextBox.Text = _order.Customer;
            OrderDateTextBlock.Text = _order.OrderDate?.ToString("dd.MM.yyyy HH:mm") ?? DateTime.Now.ToString("dd.MM.yyyy HH:mm"); OrderItemsDataGrid.ItemsSource = _order.Items;
            UpdateTotalAmount();
        }

        private void UpdateTotalAmount()
        {
            TotalAmountTextBlock.Text = $"Итого: {_order.TotalAmount:C}";
        }

        private async void CreateOrder_Click(object sender, RoutedEventArgs e)
        {
            if (!_order.Items.Any())
            {
                MessageBox.Show("Заказ не содержит позиций.", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_order.Items.Any(item => item.Quantity <= 0))
            {
                MessageBox.Show("Количество всех позиций должно быть больше нуля.", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _order.Status = "Новый"; // Меняем статус с "Формируется" на "Новый"
                var orderId = await _orderService.CreateOrderAsync(_order);
                
                MessageBox.Show($"Заказ №{orderId} успешно создан!\nОбщая сумма: {_order.TotalAmount:C}\n\nВаш заказ передан на обработку.", 
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании заказа: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
