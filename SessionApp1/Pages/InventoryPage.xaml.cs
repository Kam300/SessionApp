using SessionApp1.Models;
using SessionApp1.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace SessionApp1.Pages
{
    /// <summary>
    /// Логика взаимодействия для InventoryPage.xaml
    /// </summary>
    public partial class InventoryPage : Page
    {
        private readonly InventoryService _inventoryService;
        private readonly DatabaseService _databaseService;
        private readonly User _currentUser;
        private ObservableCollection<InventoryDocumentItem> _inventoryItems;
        private InventoryDocument _currentDocument;
        private bool _isDocumentProcessed = false;
        private bool _isNewDocument = true;

        public InventoryPage(DatabaseService databaseService, User currentUser)
        {
            InitializeComponent();

            _databaseService = databaseService;
            _inventoryService = new InventoryService(databaseService);
            _currentUser = currentUser;
            _inventoryItems = new ObservableCollection<InventoryDocumentItem>();

            // Инициализация документа
            InitializeDocument();

            // Настройка DataGrid
            SetupDataGrid();

            // Проверка прав пользователя
            CheckUserPermissions();
        }

        // Конструктор для открытия существующего документа
        public InventoryPage(DatabaseService databaseService, User currentUser, int documentId) : this(databaseService, currentUser)
        {
            _isNewDocument = false;
            LoadDocument(documentId);
        }

        private async void InitializeDocument()
        {
            if (_isNewDocument)
            {
                _currentDocument = new InventoryDocument
                {
                    DocumentDate = DateTime.Today,
                    CreatedBy = _currentUser.FullName,
                    CreatedDate = DateTime.Now,
                    WarehouseKeeper = _currentUser.FullName
                };

                // Генерация номера документа
                try
                {
                    _currentDocument.DocumentNumber = await _inventoryService.GenerateInventoryDocumentNumberAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка генерации номера документа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    _currentDocument.DocumentNumber = $"ИНВ-{DateTime.Now.Year}-{new Random().Next(1000, 9999)}";
                }

                // Заполнение полей формы
                DocumentNumberTextBox.Text = _currentDocument.DocumentNumber;
                DocumentDatePicker.SelectedDate = _currentDocument.DocumentDate;
                WarehouseKeeperTextBox.Text = _currentDocument.WarehouseKeeper;

                // Загрузка текущих остатков
                await LoadCurrentStock();
            }
        }

        private async Task LoadCurrentStock()
        {
            try
            {
                var stockItems = await _inventoryService.GetCurrentStockForInventoryAsync();
                _inventoryItems.Clear();

                foreach (var item in stockItems.Values)
                {
                    _inventoryItems.Add(item);
                }

                InventoryItemsDataGrid.ItemsSource = _inventoryItems;
                UpdateTotals();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки текущих остатков: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadDocument(int documentId)
        {
            try
            {
                _currentDocument = await _inventoryService.GetInventoryDocumentByIdAsync(documentId);
                if (_currentDocument == null)
                {
                    MessageBox.Show("Документ не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Заполнение полей формы
                DocumentNumberTextBox.Text = _currentDocument.DocumentNumber;
                DocumentDatePicker.SelectedDate = _currentDocument.DocumentDate;
                WarehouseKeeperTextBox.Text = _currentDocument.WarehouseKeeper;

                // Загрузка позиций документа
                _inventoryItems = new ObservableCollection<InventoryDocumentItem>(_currentDocument.Items);
                InventoryItemsDataGrid.ItemsSource = _inventoryItems;

                // Обновление итогов
                UpdateTotals();

                // Проверка статуса документа
                _isDocumentProcessed = _currentDocument.IsProcessed;
                if (_isDocumentProcessed)
                {
                    DisableEditing();
                }

                // Проверка необходимости утверждения
                if (_currentDocument.DifferencePercent > 20 && !_currentDocument.IsApproved && !_currentDocument.IsProcessed)
                {
                    WarningTextBlock.Text = "Расхождение превышает 20%. Требуется утверждение директором.";
                    WarningTextBlock.Visibility = Visibility.Visible;
                    ApproveButton.Visibility = _currentUser.RoleName.ToLower() == "дирекция" ? Visibility.Visible : Visibility.Collapsed; // Показываем кнопку только директору
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки документа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetupDataGrid()
        {
            // Настройка DataGrid
            InventoryItemsDataGrid.ItemsSource = _inventoryItems;

            // Добавление конвертеров для отображения
            Resources.Add("MaterialTypeConverter", new MaterialTypeConverter());
            Resources.Add("DifferenceColorConverter", new DifferenceColorConverter());
        }

        private void CheckUserPermissions()
        {
            // Проверка прав пользователя
            if (_currentUser.RoleName.ToLower() != "дирекция") // Не директор
            {
                ApproveButton.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateTotals()
        {
            // Обновление итогов
            decimal totalAccountingAmount = _inventoryItems.Sum(i => i.AccountingAmount);
            decimal totalActualAmount = _inventoryItems.Sum(i => i.ActualAmount);
            decimal totalDifferenceAmount = totalActualAmount - totalAccountingAmount;
            decimal differencePercent = totalAccountingAmount != 0 ? Math.Abs(totalDifferenceAmount / totalAccountingAmount * 100) : 0;

            TotalAccountingAmountTextBlock.Text = totalAccountingAmount.ToString("N2") + " руб.";
            TotalActualAmountTextBlock.Text = totalActualAmount.ToString("N2") + " руб.";
            TotalDifferenceAmountTextBlock.Text = totalDifferenceAmount.ToString("N2") + " руб.";
            DifferencePercentTextBlock.Text = differencePercent.ToString("N2") + "%";

            // Установка цвета для разницы
            if (totalDifferenceAmount < 0)
            {
                TotalDifferenceAmountTextBlock.Foreground = Brushes.Red;
            }
            else if (totalDifferenceAmount > 0)
            {
                TotalDifferenceAmountTextBlock.Foreground = Brushes.Green;
            }
            else
            {
                TotalDifferenceAmountTextBlock.Foreground = Brushes.Black;
            }

            // Проверка необходимости утверждения
            if (differencePercent > 20)
            {
                WarningTextBlock.Text = "Расхождение превышает 20%. Требуется утверждение директором.";
                WarningTextBlock.Visibility = Visibility.Visible;
                ApproveButton.Visibility = _currentUser.RoleName.ToLower() == "дирекция" ? Visibility.Visible : Visibility.Collapsed; // Показываем кнопку только директору
            }
            else
            {
                WarningTextBlock.Visibility = Visibility.Collapsed;
                ApproveButton.Visibility = Visibility.Collapsed;
            }

            // Обновление данных документа
            _currentDocument.TotalAccountingAmount = totalAccountingAmount;
            _currentDocument.TotalActualAmount = totalActualAmount;
            _currentDocument.DifferenceAmount = totalDifferenceAmount;
            _currentDocument.DifferencePercent = differencePercent;
        }

        private void DisableEditing()
        {
            // Отключение редактирования
            DocumentDatePicker.IsEnabled = false;
            WarehouseKeeperTextBox.IsReadOnly = true;
            InventoryItemsDataGrid.IsReadOnly = true;
            SaveButton.IsEnabled = false;
            ProcessButton.IsEnabled = false;
            ApproveButton.IsEnabled = false;
        }

        private void InventoryItemsDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var item = e.Row.Item as InventoryDocumentItem;
                if (item != null && e.Column.Header.ToString() == "Фактическое кол-во")
                {
                    var textBox = e.EditingElement as TextBox;
                    if (textBox != null && decimal.TryParse(textBox.Text, out decimal actualQuantity))
                    {
                        item.ActualQuantity = actualQuantity;
                        UpdateTotals();
                    }
                }
            }
        }

        private void DocumentDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DocumentDatePicker.SelectedDate.HasValue)
            {
                _currentDocument.DocumentDate = DocumentDatePicker.SelectedDate.Value;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(DocumentNumberTextBox.Text))
                {
                    MessageBox.Show("Номер документа не может быть пустым", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!DocumentDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Выберите дату документа", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(WarehouseKeeperTextBox.Text))
                {
                    MessageBox.Show("Укажите кладовщика", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Обновление данных документа
                _currentDocument.DocumentNumber = DocumentNumberTextBox.Text;
                _currentDocument.DocumentDate = DocumentDatePicker.SelectedDate.Value;
                _currentDocument.WarehouseKeeper = WarehouseKeeperTextBox.Text;
                _currentDocument.Items = new ObservableCollection<InventoryDocumentItem>(_inventoryItems);

                // Сохранение документа
                int documentId = await _inventoryService.SaveInventoryDocumentAsync(_currentDocument);
                _currentDocument.Id = documentId;
                _isNewDocument = false;

                MessageBox.Show("Документ успешно сохранен", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения документа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ProcessButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка, сохранен ли документ
                if (_isNewDocument || _currentDocument.Id == 0)
                {
                    MessageBox.Show("Сначала сохраните документ", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Проверка, требуется ли утверждение директором
                if (_currentDocument.DifferencePercent > 20 && !_currentDocument.IsApproved)
                {
                    MessageBox.Show("Документ с расхождением более 20% должен быть утвержден директором", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Подтверждение проведения
                var result = MessageBox.Show("Вы уверены, что хотите провести документ? После проведения документ нельзя будет изменить.", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                // Проведение документа
                await _inventoryService.ProcessInventoryDocumentAsync(_currentDocument.Id);
                _isDocumentProcessed = true;
                _currentDocument.IsProcessed = true;

                // Отключение редактирования
                DisableEditing();

                MessageBox.Show("Документ успешно проведен", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка проведения документа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ApproveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка, сохранен ли документ
                if (_isNewDocument || _currentDocument.Id == 0)
                {
                    MessageBox.Show("Сначала сохраните документ", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Подтверждение утверждения
                var result = MessageBox.Show("Вы уверены, что хотите утвердить документ?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                // Утверждение документа
                await _inventoryService.ApproveInventoryDocumentAsync(_currentDocument.Id, _currentUser.FullName);
                _currentDocument.IsApproved = true;
                _currentDocument.ApprovedBy = _currentUser.FullName;
                _currentDocument.ApprovedDate = DateTime.Now;

                // Скрытие кнопки утверждения
                ApproveButton.Visibility = Visibility.Collapsed;
                WarningTextBlock.Text = "Документ утвержден директором.";

                MessageBox.Show("Документ успешно утвержден", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка утверждения документа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создание окна печати
                var printWindow = new SessionApp1.Views.PrintPreviewWindow("Инвентаризация", GeneratePrintContent());
                printWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка печати документа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Заголовок
            var title = new TextBlock
            {
                Text = "ИНВЕНТАРИЗАЦИОННАЯ ВЕДОМОСТЬ",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(title, 0);
            grid.Children.Add(title);

            // Информация о документе
            var docInfo = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 10) };
            docInfo.Children.Add(new TextBlock { Text = $"Номер документа: {_currentDocument.DocumentNumber}", Margin = new Thickness(0, 0, 0, 5) });
            docInfo.Children.Add(new TextBlock { Text = $"Дата: {_currentDocument.DocumentDate:dd.MM.yyyy}", Margin = new Thickness(0, 0, 0, 5) });
            docInfo.Children.Add(new TextBlock { Text = $"Кладовщик: {_currentDocument.WarehouseKeeper}", Margin = new Thickness(0, 0, 0, 5) });
            
            if (_currentDocument.IsApproved)
            {
                docInfo.Children.Add(new TextBlock { Text = $"Утверждено: {_currentDocument.ApprovedBy} ({_currentDocument.ApprovedDate:dd.MM.yyyy HH:mm})", Margin = new Thickness(0, 0, 0, 5) });
            }
            
            Grid.SetRow(docInfo, 1);
            grid.Children.Add(docInfo);

            // Таблица материалов
            var table = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                GridLinesVisibility = DataGridGridLinesVisibility.All,
                Margin = new Thickness(0, 0, 0, 10),
                ItemsSource = _inventoryItems
            };

            table.Columns.Add(new DataGridTextColumn { Header = "Артикул", Binding = new Binding("MaterialArticle"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Наименование", Binding = new Binding("MaterialName"), Width = new DataGridLength(2, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Учетное кол-во", Binding = new Binding("AccountingQuantity") { StringFormat = "N3" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Фактическое кол-во", Binding = new Binding("ActualQuantity") { StringFormat = "N3" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Разница", Binding = new Binding("DifferenceQuantity") { StringFormat = "N3" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Ед. изм.", Binding = new Binding("Unit"), Width = new DataGridLength(0.5, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Цена", Binding = new Binding("Price") { StringFormat = "N2" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Учетная сумма", Binding = new Binding("AccountingAmount") { StringFormat = "N2" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Фактическая сумма", Binding = new Binding("ActualAmount") { StringFormat = "N2" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            table.Columns.Add(new DataGridTextColumn { Header = "Разница суммы", Binding = new Binding("DifferenceAmount") { StringFormat = "N2" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });

            Grid.SetRow(table, 2);
            grid.Children.Add(table);

            // Итоги
            var totals = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 10) };
            totals.Children.Add(new TextBlock { Text = $"Учетная сумма: {_currentDocument.TotalAccountingAmount:N2} руб.", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
            totals.Children.Add(new TextBlock { Text = $"Фактическая сумма: {_currentDocument.TotalActualAmount:N2} руб.", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
            totals.Children.Add(new TextBlock { Text = $"Разница: {_currentDocument.DifferenceAmount:N2} руб.", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
            totals.Children.Add(new TextBlock { Text = $"Процент расхождения: {_currentDocument.DifferencePercent:N2}%", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
            Grid.SetRow(totals, 3);
            grid.Children.Add(totals);

            // Подписи
            var signatures = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 20, 0, 0) };
            signatures.Children.Add(new TextBlock { Text = "Кладовщик: __________________ / " + _currentDocument.WarehouseKeeper, Margin = new Thickness(0, 0, 0, 10) });
            
            if (_currentDocument.IsApproved)
            {
                signatures.Children.Add(new TextBlock { Text = "Утверждено: __________________ / " + _currentDocument.ApprovedBy, Margin = new Thickness(0, 0, 0, 10) });
            }
            
            Grid.SetRow(signatures, 4);
            grid.Children.Add(signatures);

            return grid;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Возврат на предыдущую страницу
            if (NavigationService != null)
            {
                NavigationService.GoBack();
            }
        }
    }

    // Конвертер для отображения типа материала
    public class MaterialTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string materialType)
            {
                return materialType == "fabric" ? "Ткань" : "Фурнитура";
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Конвертер для отображения цвета разницы
    public class DifferenceColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is decimal difference)
            {
                if (difference < 0)
                {
                    return Brushes.Red;
                }
                else if (difference > 0)
                {
                    return Brushes.Green;
                }
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}