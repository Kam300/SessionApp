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
    /// Логика взаимодействия для MaterialMovementReportPage.xaml
    /// </summary>
    public partial class MaterialMovementReportPage : Page
    {
        private readonly InventoryService _inventoryService;
        private readonly DatabaseService _databaseService;
        private readonly User _currentUser;
        private List<MaterialMovementReport> _movementItems;

        public MaterialMovementReportPage(DatabaseService databaseService, User currentUser)
        {
            InitializeComponent();

            _databaseService = databaseService;
            _inventoryService = new InventoryService(databaseService);
            _currentUser = currentUser;

            // Установка начальных дат (текущий месяц)
            var today = DateTime.Today;
            StartDatePicker.SelectedDate = new DateTime(today.Year, today.Month, 1);
            EndDatePicker.SelectedDate = today;

            // Загрузка данных
            LoadMovementData();
        }

        private async void LoadMovementData()
        {
            try
            {
                // Проверка дат
                if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Выберите период для отчета", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Получение данных о движении
                DateTime startDate = StartDatePicker.SelectedDate.Value;
                DateTime endDate = EndDatePicker.SelectedDate.Value.AddDays(1).AddSeconds(-1); // До конца дня

                _movementItems = await _inventoryService.GetMaterialMovementReportAsync(startDate, endDate);
                
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
            if (_movementItems == null)
                return;

            // Фильтрация по типу материала
            var filteredItems = _movementItems;
            
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
            MovementDataGrid.ItemsSource = filteredItems;

            // Сортировка по типу и артикулу
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(MovementDataGrid.ItemsSource);
            if (view != null)
            {
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new System.ComponentModel.SortDescription("Type", System.ComponentModel.ListSortDirection.Ascending));
                view.SortDescriptions.Add(new System.ComponentModel.SortDescription("Article", System.ComponentModel.ListSortDirection.Ascending));
            }
        }

        private void ApplyFilterButton_Click(object sender, RoutedEventArgs e)
        {
            LoadMovementData();
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создание окна печати
                var printWindow = new SessionApp1.Views.PrintPreviewWindow("Отчет по движению материалов", GeneratePrintContent());
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
                Text = "ОТЧЕТ ПО ДВИЖЕНИЮ МАТЕРИАЛОВ",
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
                ItemsSource = MovementDataGrid.ItemsSource
            };

            table.Columns.Add(new DataGridTextColumn { Header = "Артикул", Binding = new Binding("Article"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Наименование", Binding = new Binding("Name"), Width = new DataGridLength(2, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Тип", Binding = new Binding("Type"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Нач. остаток", Binding = new Binding("InitialQuantity") { StringFormat = "N3" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Приход", Binding = new Binding("ReceiptQuantity") { StringFormat = "N3" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Расход", Binding = new Binding("ExpenseQuantity") { StringFormat = "N3" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Кон. остаток", Binding = new Binding("FinalQuantity") { StringFormat = "N3" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Ед. изм.", Binding = new Binding("Unit"), Width = new DataGridLength(0.5, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Нач. сумма", Binding = new Binding("InitialAmount") { StringFormat = "N2" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Приход сумма", Binding = new Binding("ReceiptAmount") { StringFormat = "N2" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Расход сумма", Binding = new Binding("ExpenseAmount") { StringFormat = "N2" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Кон. сумма", Binding = new Binding("FinalAmount") { StringFormat = "N2" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });

            Grid.SetRow(table, 2);
            grid.Children.Add(table);

            // Итоги
            var items = (IEnumerable<MaterialMovementReport>)MovementDataGrid.ItemsSource;
            if (items != null)
            {
                var totals = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 10, 0, 0) };
                decimal initialTotal = items.Sum(i => i.InitialAmount);
                decimal receiptTotal = items.Sum(i => i.ReceiptAmount);
                decimal expenseTotal = items.Sum(i => i.ExpenseAmount);
                decimal finalTotal = items.Sum(i => i.FinalAmount);
                
                totals.Children.Add(new TextBlock { Text = $"Начальный остаток: {initialTotal:N2} руб.", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
                totals.Children.Add(new TextBlock { Text = $"Приход: {receiptTotal:N2} руб.", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
                totals.Children.Add(new TextBlock { Text = $"Расход: {expenseTotal:N2} руб.", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
                totals.Children.Add(new TextBlock { Text = $"Конечный остаток: {finalTotal:N2} руб.", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
                
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
                    FileName = $"Движение_материалов_{StartDatePicker.SelectedDate:yyyyMMdd}-{EndDatePicker.SelectedDate:yyyyMMdd}"
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
            var items = (IEnumerable<MaterialMovementReport>)MovementDataGrid.ItemsSource;
            if (items == null)
                return;

            // Создание CSV файла
            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                // Заголовок
                writer.WriteLine("Артикул;Наименование;Тип;Нач. остаток;Приход;Расход;Кон. остаток;Ед. изм.;Нач. сумма;Приход сумма;Расход сумма;Кон. сумма");

                // Данные
                foreach (var item in items)
                {
                    writer.WriteLine($"{item.Article};{item.Name};{item.Type};"
                        + $"{item.InitialQuantity.ToString("N3").Replace(',', '.')};"
                        + $"{item.ReceiptQuantity.ToString("N3").Replace(',', '.')};"
                        + $"{item.ExpenseQuantity.ToString("N3").Replace(',', '.')};"
                        + $"{item.FinalQuantity.ToString("N3").Replace(',', '.')};"
                        + $"{item.Unit};"
                        + $"{item.InitialAmount.ToString("N2").Replace(',', '.')};"
                        + $"{item.ReceiptAmount.ToString("N2").Replace(',', '.')};"
                        + $"{item.ExpenseAmount.ToString("N2").Replace(',', '.')};"
                        + $"{item.FinalAmount.ToString("N2").Replace(',', '.')}");
                }

                // Итоги
                decimal initialTotal = items.Sum(i => i.InitialAmount);
                decimal receiptTotal = items.Sum(i => i.ReceiptAmount);
                decimal expenseTotal = items.Sum(i => i.ExpenseAmount);
                decimal finalTotal = items.Sum(i => i.FinalAmount);
                
                writer.WriteLine($"ИТОГО;;;;;;;"
                    + $"{initialTotal.ToString("N2").Replace(',', '.')};"
                    + $"{receiptTotal.ToString("N2").Replace(',', '.')};"
                    + $"{expenseTotal.ToString("N2").Replace(',', '.')};"
                    + $"{finalTotal.ToString("N2").Replace(',', '.')}");
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