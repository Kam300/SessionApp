using SessionApp1.Models;
using SessionApp1.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SessionApp1.Views
{
    public partial class ReceiptDocumentWindow : Window
    {
        private readonly DatabaseService _dbService;
        private Dictionary<string, string> _allMaterials;
        private Dictionary<string, MaterialInfo> _allMaterialsWithUnits;
        private ObservableCollection<ReceiptDocumentItemViewModel> _documentItems;

        public ReceiptDocumentWindow()
        {
            InitializeComponent();
            _dbService = new DatabaseService();
            InitializeDocumentItems();

            DocNumberTextBox.Text = $"ПРИХ-{DateTime.Now:yyyyMMdd}-{DateTime.Now:HHmmss}";
            DocDatePicker.SelectedDate = DateTime.Now;

            Loaded += ReceiptDocumentWindow_Loaded;
        }

        private void InitializeDocumentItems()
        {
            _documentItems = new ObservableCollection<ReceiptDocumentItemViewModel>();
            _documentItems.CollectionChanged += (s, e) => UpdateTotalSum();

            _documentItems.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (ReceiptDocumentItemViewModel item in e.NewItems)
                    {
                        item.PropertyChanged += (sender, args) => UpdateTotalSum();

                        // ДОБАВЛЕНО: Подписываемся на изменение материала для автоподстановки единицы
                        item.PropertyChanged += (sender, args) =>
                        {
                            if (args.PropertyName == nameof(ReceiptDocumentItemViewModel.MaterialArticle))
                            {
                                UpdateMaterialUnit(item);
                            }
                        };
                    }
                }
            };

            ItemsDataGrid.ItemsSource = _documentItems;
        }

        private async void ReceiptDocumentWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Cursor = System.Windows.Input.Cursors.Wait;

                // ИСПРАВЛЕНО: Загружаем материалы с единицами измерения
                _allMaterialsWithUnits = await _dbService.GetAllMaterialsWithUnitsAsync();
                _allMaterials = _allMaterialsWithUnits.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Name);

                if (_allMaterials != null && _allMaterials.Any())
                {
                    MaterialComboColumn.ItemsSource = _allMaterials;
                    AddNewRow();
                    System.Diagnostics.Debug.WriteLine($"Успешно установлен ItemsSource с {_allMaterials.Count} материалами");
                }
                else
                {
                    MessageBox.Show("В базе данных не найдено ни одного материала.\nПроверьте, что таблицы 'fabrics' и 'fittings' содержат данные.",
                        "Нет данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                UpdateTotalSum();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки материалов: {ex.Message}\n\nПроверьте подключение к базе данных.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Детали ошибки: {ex}");
            }
            finally
            {
                this.Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        // НОВЫЙ МЕТОД: Автоматически устанавливает единицу измерения при выборе материала
        private void UpdateMaterialUnit(ReceiptDocumentItemViewModel item)
        {
            if (!string.IsNullOrEmpty(item.MaterialArticle) && _allMaterialsWithUnits != null)
            {
                if (_allMaterialsWithUnits.TryGetValue(item.MaterialArticle, out var materialInfo))
                {
                    item.MaterialName = materialInfo.Name;
                    item.Unit = materialInfo.Unit; // Автоматически устанавливаем единицу измерения
                }
            }
        }

        private void AddRowButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewRow();
        }

        private void AddNewRow()
        {
            try
            {
                ItemsDataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                var newItem = new ReceiptDocumentItemViewModel();
                newItem.PropertyChanged += (sender, args) => UpdateTotalSum();
                newItem.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(ReceiptDocumentItemViewModel.MaterialArticle))
                    {
                        UpdateMaterialUnit(newItem);
                    }
                };
                _documentItems.Add(newItem);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка добавления строки: {ex.Message}");
            }
        }

        private void UpdateTotalSum()
        {
            try
            {
                var total = _documentItems.Sum(item => item.TotalAmount);
                TotalSumTextBlock.Text = $"Итого: {total:C}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления суммы: {ex.Message}");
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ItemsDataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                ItemsDataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            }
            catch
            {
                ItemsDataGrid.CancelEdit(DataGridEditingUnit.Row);
                ItemsDataGrid.CancelEdit(DataGridEditingUnit.Cell);
            }

            if (string.IsNullOrWhiteSpace(DocNumberTextBox.Text) || DocDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Пожалуйста, заполните номер и дату документа.",
                    "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var validItems = _documentItems
                .Where(i => !string.IsNullOrWhiteSpace(i.MaterialArticle) && i.Quantity > 0 && i.Price > 0)
                .ToList();

            if (!validItems.Any())
            {
                MessageBox.Show("Добавьте хотя бы один материал с указанным количеством и ценой.",
                    "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var document = new ReceiptDocument
            {
                DocumentNumber = DocNumberTextBox.Text,
                DocumentDate = DocDatePicker.SelectedDate.Value,
                Supplier = SupplierTextBox.Text
            };

            var itemsToSave = validItems.Select(vm => new ReceiptDocumentItem
            {
                MaterialArticle = vm.MaterialArticle,
                MaterialName = vm.MaterialName,
                Quantity = vm.Quantity,
                Price = vm.Price
            }).ToList();

            try
            {
                await _dbService.SaveReceiptDocumentAsync(document, itemsToSave);
                MessageBox.Show($"Документ '{document.DocumentNumber}' успешно проведен!\n" +
                              $"Проведено позиций: {itemsToSave.Count}\n" +
                              $"Общая сумма: {itemsToSave.Sum(i => i.TotalAmount):C}\n" +
                              $"Остатки материалов обновлены.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения документа: {ex.Message}",
                    "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // ОБНОВЛЕННАЯ ViewModel с поддержкой Unit
    public class ReceiptDocumentItemViewModel : INotifyPropertyChanged
    {
        private string _materialArticle;
        private string _materialName;
        private decimal _quantity;
        private string _unit;
        private decimal _price;

        public string MaterialArticle
        {
            get => _materialArticle;
            set
            {
                if (_materialArticle != value)
                {
                    _materialArticle = value;
                    OnPropertyChanged(nameof(MaterialArticle));
                    OnPropertyChanged(nameof(TotalAmount));
                }
            }
        }

        public string MaterialName
        {
            get => _materialName;
            set
            {
                if (_materialName != value)
                {
                    _materialName = value;
                    OnPropertyChanged(nameof(MaterialName));
                }
            }
        }

        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged(nameof(Quantity));
                    OnPropertyChanged(nameof(TotalAmount));
                }
            }
        }

        public string Unit
        {
            get => _unit;
            set
            {
                if (_unit != value)
                {
                    _unit = value;
                    OnPropertyChanged(nameof(Unit));
                }
            }
        }

        public decimal Price
        {
            get => _price;
            set
            {
                if (_price != value)
                {
                    _price = value;
                    OnPropertyChanged(nameof(Price));
                    OnPropertyChanged(nameof(TotalAmount));
                }
            }
        }

        public decimal TotalAmount => Quantity * Price;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
