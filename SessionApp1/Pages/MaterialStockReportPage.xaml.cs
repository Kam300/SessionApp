using Microsoft.Win32;
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

namespace SessionApp1.Pages
{
    /// <summary>
    /// Логика взаимодействия для MaterialStockReportPage.xaml
    /// </summary>
    public partial class MaterialStockReportPage : Page
    {
        private readonly InventoryService _inventoryService;
        private readonly DatabaseService _databaseService;
        private readonly User _currentUser;
        private List<MaterialStockReport> _allStockItems;

        public MaterialStockReportPage(DatabaseService databaseService, User currentUser)
        {
            InitializeComponent();

            _databaseService = databaseService;
            _inventoryService = new InventoryService(databaseService);
            _currentUser = currentUser;

            // Загрузка данных
            LoadStockData();
        }

        private async void LoadStockData()
        {
            try
            {
                // Получение данных об остатках
                _allStockItems = await _inventoryService.GetMaterialStockReportAsync();
                
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
            if (_allStockItems == null)
                return;

            // Фильтрация по типу материала
            var filteredItems = _allStockItems;
            
            if (MaterialTypeComboBox.SelectedIndex == 1) // Ткани
            {
                filteredItems = filteredItems.Where(i => i.Type == "Ткань").ToList();
            }
            else if (MaterialTypeComboBox.SelectedIndex == 2) // Фурнитура
            {
                filteredItems = filteredItems.Where(i => i.Type == "Фурнитура").ToList();
            }

            // Фильтрация по артикулу
            if (!string.IsNullOrWhiteSpace(ArticleFilterTextBox.Text))
            {
                string articleFilter = ArticleFilterTextBox.Text.Trim().ToLower();
                filteredItems = filteredItems.Where(i => i.Article.ToLower().Contains(articleFilter)).ToList();
            }

            // Отображение отфильтрованных данных
            StockDataGrid.ItemsSource = filteredItems;

            // Сортировка по типу и артикулу
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(StockDataGrid.ItemsSource);
            if (view != null)
            {
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new System.ComponentModel.SortDescription("Type", System.ComponentModel.ListSortDirection.Ascending));
                view.SortDescriptions.Add(new System.ComponentModel.SortDescription("Article", System.ComponentModel.ListSortDirection.Ascending));
            }
        }

        private void ApplyFilterButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создание окна печати
                var printWindow = new SessionApp1.Views.PrintPreviewWindow("Отчет по остаткам материалов", GeneratePrintContent());
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
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Заголовок
            var title = new TextBlock
            {
                Text = "ОТЧЕТ ПО ОСТАТКАМ МАТЕРИАЛОВ",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(title, 0);
            grid.Children.Add(title);

            // Информация об отчете
            var reportInfo = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 10) };
            reportInfo.Children.Add(new TextBlock { Text = $"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}", Margin = new Thickness(0, 0, 0, 5) });
            
            string filterInfo = "Фильтр: ";
            if (MaterialTypeComboBox.SelectedIndex == 0)
                filterInfo += "Все материалы";
            else if (MaterialTypeComboBox.SelectedIndex == 1)
                filterInfo += "Только ткани";
            else if (MaterialTypeComboBox.SelectedIndex == 2)
                filterInfo += "Только фурнитура";
            
            if (!string.IsNullOrWhiteSpace(ArticleFilterTextBox.Text))
                filterInfo += $", артикул содержит '{ArticleFilterTextBox.Text}'";
            
            reportInfo.Children.Add(new TextBlock { Text = filterInfo, Margin = new Thickness(0, 0, 0, 5) });
            Grid.SetRow(reportInfo, 1);
            grid.Children.Add(reportInfo);

            // Таблица материалов
            var table = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                GridLinesVisibility = DataGridGridLinesVisibility.All,
                Margin = new Thickness(0, 0, 0, 10),
                ItemsSource = StockDataGrid.ItemsSource
            };

            table.Columns.Add(new DataGridTextColumn { Header = "Артикул", Binding = new Binding("Article"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Наименование", Binding = new Binding("Name"), Width = new DataGridLength(2, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Тип", Binding = new Binding("Type"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Количество", Binding = new Binding("Quantity") { StringFormat = "N3" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Ед. изм.", Binding = new Binding("Unit"), Width = new DataGridLength(0.5, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Цена", Binding = new Binding("Price") { StringFormat = "N2" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Сумма", Binding = new Binding("Amount") { StringFormat = "N2" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });

            Grid.SetRow(table, 2);
            grid.Children.Add(table);

            // Итоги
            var items = (IEnumerable<MaterialStockReport>)StockDataGrid.ItemsSource;
            if (items != null)
            {
                var totals = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 10, 0, 0) };
                decimal totalAmount = items.Sum(i => i.Amount);
                totals.Children.Add(new TextBlock { Text = $"Итого: {totalAmount:N2} руб.", FontWeight = FontWeights.Bold });
                Grid.SetRow(totals, 3);
                grid.Children.Add(totals);
            }

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
                    FileName = $"Остатки_материалов_{DateTime.Now:yyyyMMdd}"
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
            var items = (IEnumerable<MaterialStockReport>)StockDataGrid.ItemsSource;
            if (items == null)
                return;

            // Создание CSV файла
            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                // Заголовок
                writer.WriteLine("Артикул;Наименование;Тип;Количество;Ед. изм.;Цена;Сумма");

                // Данные
                foreach (var item in items)
                {
                    writer.WriteLine($"{item.Article};{item.Name};{item.Type};{item.Quantity.ToString("N3").Replace(',', '.')};{item.Unit};{item.Price.ToString("N2").Replace(',', '.')};{item.Amount.ToString("N2").Replace(',', '.')}");
                }

                // Итоги
                decimal totalAmount = items.Sum(i => i.Amount);
                writer.WriteLine($";;;;;;{totalAmount.ToString("N2").Replace(',', '.')}");
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