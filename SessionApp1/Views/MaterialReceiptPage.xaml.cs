using SessionApp1.Services;
using SessionApp1.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SessionApp1.Views
{
    public partial class MaterialReceiptPage : Page
    {
        private readonly MaterialAccountingService _materialService;
        private readonly DatabaseService _databaseService;
        private ObservableCollection<MaterialReceiptItem> _receiptItems;
        private bool _isDocumentProcessed = false;

        public MaterialReceiptPage()
        {
            InitializeComponent();
            _materialService = new MaterialAccountingService();
            _databaseService = new DatabaseService();
            _receiptItems = new ObservableCollection<MaterialReceiptItem>();

            InitializeDocument();
            SetupDataGrid();
        }

        private void InitializeDocument()
        {
            DocumentNumberTextBox.Text = GenerateDocumentNumber();
            ReceiptDatePicker.SelectedDate = DateTime.Today;
            SupplierTextBox.Text = "";
            UpdateTotalAmount();
        }

        private void SetupDataGrid()
        {
            ReceiptItemsDataGrid.ItemsSource = _receiptItems;
            _receiptItems.CollectionChanged += (s, e) => UpdateTotalAmount();
            ReceiptItemsDataGrid.CellEditEnding += ReceiptItemsDataGrid_CellEditEnding;
        }

        private string GenerateDocumentNumber()
        {
            return $"ПМ-{DateTime.Now:yyyyMMdd}-{DateTime.Now:HHmmss}";
        }

        private void AddItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isDocumentProcessed)
            {
                MessageBox.Show("Документ уже проведен и не может быть изменен.",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newItem = new MaterialReceiptItem
            {
                MaterialArticle = "",
                MaterialType = "fabric",
                Quantity = 1,
                Price = 0,
                Amount = 0
            };

            _receiptItems.Add(newItem);
        }

        private void ReceiptItemsDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (_isDocumentProcessed) return;

            if (e.Column.Header.ToString() == "Количество" || e.Column.Header.ToString() == "Цена")
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (e.Row.Item is MaterialReceiptItem item)
                    {
                        item.Amount = item.Quantity * item.Price;
                        UpdateTotalAmount();
                        ReceiptItemsDataGrid.Items.Refresh();
                    }
                }));
            }
        }

        private void UpdateTotalAmount()
        {
            var total = _receiptItems.Sum(item => item.Amount);
            TotalAmountText.Text = $"Общая сумма: {total:C}";
        }

        private async void ProcessDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isDocumentProcessed)
            {
                MessageBox.Show("Документ уже проведен.",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(DocumentNumberTextBox.Text))
            {
                MessageBox.Show("Укажите номер документа.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(SupplierTextBox.Text))
            {
                MessageBox.Show("Укажите поставщика.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!ReceiptDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Укажите дату поступления.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!_receiptItems.Any())
            {
                MessageBox.Show("Добавьте хотя бы один материал.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            foreach (var item in _receiptItems)
            {
                if (string.IsNullOrWhiteSpace(item.MaterialArticle))
                {
                    MessageBox.Show("Заполните артикул для всех материалов.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (item.Quantity <= 0)
                {
                    MessageBox.Show("Количество должно быть больше нуля.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (item.Price <= 0)
                {
                    MessageBox.Show("Цена должна быть больше нуля.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            try
            {
                var receipt = new MaterialReceipt
                {
                    DocumentNumber = DocumentNumberTextBox.Text,
                    ReceiptDate = ReceiptDatePicker.SelectedDate.Value,
                    Supplier = SupplierTextBox.Text,
                    TotalAmount = _receiptItems.Sum(i => i.Amount),
                    IsProcessed = true,
                    Items = _receiptItems.ToList()
                };

                await ProcessReceiptAsync(receipt);

                _isDocumentProcessed = true;

                DocumentNumberTextBox.IsEnabled = false;
                ReceiptDatePicker.IsEnabled = false;
                SupplierTextBox.IsEnabled = false;
                AddItemButton.IsEnabled = false;
                ReceiptItemsDataGrid.IsReadOnly = true;
                ProcessDocumentButton.IsEnabled = false;

                MessageBox.Show($"Документ {receipt.DocumentNumber} успешно проведен!\n" +
                               $"Поступило материалов на сумму: {receipt.TotalAmount:C}",
                               "Документ проведен", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка проведения документа: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ProcessReceiptAsync(MaterialReceipt receipt)
        {
            await Task.Delay(1000);

            System.Diagnostics.Debug.WriteLine($"Проведен документ поступления:");
            System.Diagnostics.Debug.WriteLine($"Номер: {receipt.DocumentNumber}");
            System.Diagnostics.Debug.WriteLine($"Дата: {receipt.ReceiptDate:dd.MM.yyyy}");
            System.Diagnostics.Debug.WriteLine($"Поставщик: {receipt.Supplier}");
            System.Diagnostics.Debug.WriteLine($"Сумма: {receipt.TotalAmount:C}");
            System.Diagnostics.Debug.WriteLine($"Позиций: {receipt.Items.Count}");

            foreach (var item in receipt.Items)
            {
                System.Diagnostics.Debug.WriteLine($"  {item.MaterialArticle} ({item.MaterialType}): " +
                                                 $"{item.Quantity} x {item.Price:C} = {item.Amount:C}");
            }
        }
    }
}
