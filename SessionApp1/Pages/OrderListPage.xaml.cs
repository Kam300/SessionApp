using Microsoft.Win32;
using SessionApp1.Helpers;
using SessionApp1.Models;
using SessionApp1.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace SessionApp1.Pages
{
    /// <summary>
    /// Логика взаимодействия для OrderListPage.xaml
    /// </summary>
    public partial class OrderListPage : Page
    {
        private readonly DatabaseService _databaseService;
        private readonly OrderService _orderService;
        private readonly User _currentUser;
        private List<OrderListItem> _allOrders;

        public OrderListPage(DatabaseService databaseService, User currentUser)
        {
            InitializeComponent();

            _databaseService = databaseService;
            _orderService = new OrderService();
            _currentUser = currentUser;

            // Установка начальных дат (последние 30 дней)
            var today = DateTime.Today;
            StartDatePicker.SelectedDate = today.AddDays(-30);
            EndDatePicker.SelectedDate = today;

            // Инициализация комбобокса статусов
            InitializeStatusComboBox();

            // Загрузка данных
            LoadOrdersData();
        }

        private void InitializeStatusComboBox()
        {
            // Добавление опции "Все статусы"
            StatusComboBox.Items.Add(new ComboBoxItem { Content = "Все статусы" });

            // Добавление всех статусов заказов
            foreach (var status in SessionApp1.Helpers.OrderStatusHelper.GetAllStatuses())
            {
                StatusComboBox.Items.Add(new ComboBoxItem { Content = status.Value, Tag = status.Key });
            }

            // Выбор первого элемента ("Все статусы")
            StatusComboBox.SelectedIndex = 0;
        }

        private async void LoadOrdersData()
        {
            try
            {
                // Проверка дат
                if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Выберите период для отчета", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Получение данных о заказах
                DateTime startDate = StartDatePicker.SelectedDate.Value;
                DateTime endDate = EndDatePicker.SelectedDate.Value.AddDays(1).AddSeconds(-1); // До конца дня

                _allOrders = await _orderService.GetOrdersAsync(startDate, endDate);
                
                // Применение фильтров
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            if (_allOrders == null)
                return;

            // Фильтрация по статусу
            var filteredOrders = _allOrders;
            
            if (StatusComboBox.SelectedIndex > 0) // Не "Все статусы"
            {
                var selectedItem = StatusComboBox.SelectedItem as ComboBoxItem;
                if (selectedItem != null && selectedItem.Tag is OrderStatus selectedStatus)
                {
                    filteredOrders = filteredOrders.Where(o => o.Status == selectedStatus).ToList();
                }
            }

            // Фильтрация по заказчику
            if (!string.IsNullOrWhiteSpace(CustomerFilterTextBox.Text))
            {
                string customerFilter = CustomerFilterTextBox.Text.Trim().ToLower();
                filteredOrders = filteredOrders.Where(o => o.CustomerName.ToLower().Contains(customerFilter)).ToList();
            }

            // Фильтрация по номеру заказа
            if (!string.IsNullOrWhiteSpace(OrderNumberFilterTextBox.Text))
            {
                string orderNumberFilter = OrderNumberFilterTextBox.Text.Trim().ToLower();
                filteredOrders = filteredOrders.Where(o => o.OrderNumber.ToLower().Contains(orderNumberFilter)).ToList();
            }

            // Отображение отфильтрованных данных
            OrdersDataGrid.ItemsSource = filteredOrders;

            // Сортировка по дате (сначала новые)
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(OrdersDataGrid.ItemsSource);
            if (view != null)
            {
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new System.ComponentModel.SortDescription("OrderDate", System.ComponentModel.ListSortDirection.Descending));
            }
        }

        private void ApplyFilterButton_Click(object sender, RoutedEventArgs e)
        {
            LoadOrdersData();
        }

        private void OrdersDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Открытие детальной информации о заказе при двойном клике
            if (OrdersDataGrid.SelectedItem is OrderListItem selectedOrder)
            {
                // Здесь должен быть код для открытия детальной информации о заказе
                MessageBox.Show($"Открытие заказа №{selectedOrder.OrderNumber}", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создание окна печати
                var printWindow = new SessionApp1.Views.PrintPreviewWindow("Список заказов", GeneratePrintContent());
                printWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка печати отчета: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private UIElement GeneratePrintContent()
        {
            // Создание содержимого для печати
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Заголовок
            var title = new TextBlock
            {
                Text = "СПИСОК ЗАКАЗОВ",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(title, 0);
            grid.Children.Add(title);

            // Информация об отчете
            var reportInfo = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 10) };
            reportInfo.Children.Add(new TextBlock { Text = $"Период: с {StartDatePicker.SelectedDate:dd.MM.yyyy} по {EndDatePicker.SelectedDate:dd.MM.yyyy}", Margin = new Thickness(0, 0, 0, 5) });
            reportInfo.Children.Add(new TextBlock { Text = $"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}", Margin = new Thickness(0, 0, 0, 5) });
            
            string filterInfo = "Фильтр: ";
            if (StatusComboBox.SelectedIndex == 0)
                filterInfo += "Все статусы";
            else
                filterInfo += $"Статус '{(StatusComboBox.SelectedItem as ComboBoxItem).Content}'";
            
            if (!string.IsNullOrWhiteSpace(CustomerFilterTextBox.Text))
                filterInfo += $", заказчик содержит '{CustomerFilterTextBox.Text}'";
            
            if (!string.IsNullOrWhiteSpace(OrderNumberFilterTextBox.Text))
                filterInfo += $", номер заказа содержит '{OrderNumberFilterTextBox.Text}'";
            
            reportInfo.Children.Add(new TextBlock { Text = filterInfo, Margin = new Thickness(0, 0, 0, 5) });
            Grid.SetRow(reportInfo, 1);
            grid.Children.Add(reportInfo);

            // Таблица заказов
            var table = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                GridLinesVisibility = DataGridGridLinesVisibility.All,
                Margin = new Thickness(0, 0, 0, 10),
                ItemsSource = OrdersDataGrid.ItemsSource
            };

            table.Columns.Add(new DataGridTextColumn { Header = "№ заказа", Binding = new Binding("OrderNumber"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Дата", Binding = new Binding("OrderDate") { StringFormat = "dd.MM.yyyy" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Заказчик", Binding = new Binding("CustomerName"), Width = new DataGridLength(2, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Менеджер", Binding = new Binding("ManagerName"), Width = new DataGridLength(2, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Кол-во изделий", Binding = new Binding("TotalItems"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Статус", Binding = new Binding("StatusName"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });

            Grid.SetRow(table, 2);
            grid.Children.Add(table);

            return grid;
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Диалог сохранения файла
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV файлы (*.csv)|*.csv",
                    DefaultExt = "csv",
                    FileName = $"Список_заказов_{StartDatePicker.SelectedDate:yyyyMMdd}-{EndDatePicker.SelectedDate:yyyyMMdd}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Экспорт в CSV
                    ExportToCsv(saveFileDialog.FileName);
                    MessageBox.Show("Экспорт успешно выполнен", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToCsv(string filePath)
        {
            var orders = (IEnumerable<OrderListItem>)OrdersDataGrid.ItemsSource;
            if (orders == null)
                return;

            // Создание CSV файла
            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                // Заголовок
                writer.WriteLine("№ заказа;Дата;Заказчик;Менеджер;Кол-во изделий;Статус");

                // Данные
                foreach (var order in orders)
                {
                    writer.WriteLine($"{order.OrderNumber};"
                        + $"{order.OrderDate:dd.MM.yyyy};"
                        + $"{order.CustomerName};"
                        + $"{order.ManagerName};"
                        + $"{order.TotalItems};"
                        + $"{order.StatusName}");
                }

                // Итоги
                int totalOrders = orders.Count();
                int totalItems = orders.Sum(o => o.TotalItems);
                writer.WriteLine($"Всего заказов: {totalOrders}, Всего изделий: {totalItems}");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Возврат на предыдущую страницу
            if (NavigationService != null)
            {
                NavigationService.GoBack();
            }
        }
    }
}