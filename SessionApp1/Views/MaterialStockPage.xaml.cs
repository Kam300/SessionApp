using SessionApp1.Services;
using SessionApp1.Models;
using System;
using System.Windows;
using System.Windows.Controls;

namespace SessionApp1.Views
{
    public partial class MaterialStockPage : Page
    {
        private MaterialAccountingService _materialService;

        public MaterialStockPage()
        {
            InitializeComponent();
            InitializeServices();
            LoadFabricStock();
        }

        private void InitializeServices()
        {
            try
            {
                _materialService = new MaterialAccountingService();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации сервиса: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MaterialType_Changed(object sender, RoutedEventArgs e)
        {
            if (_materialService == null)
            {
                InitializeServices();
            }

            if (FabricsRadio.IsChecked == true)
            {
                LoadFabricStock();
            }
            else
            {
                LoadFittingStock();
            }
        }

        private async void LoadFabricStock()
        {
            try
            {
                if (_materialService == null)
                {
                    InitializeServices();
                }

                var stocks = await _materialService.GetFabricStockWithUnitsAsync();

                MaterialStockDataGrid.Columns.Clear();
                MaterialStockDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Рулон",
                    Binding = new System.Windows.Data.Binding("RollId"),
                    Width = 80
                });
                MaterialStockDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Артикул",
                    Binding = new System.Windows.Data.Binding("FabricArticle"),
                    Width = 120
                });
                MaterialStockDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Название",
                    Binding = new System.Windows.Data.Binding("FabricName"),
                    Width = 150
                });
                MaterialStockDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Длина (мм)",
                    Binding = new System.Windows.Data.Binding("LengthMm"),
                    Width = 100
                });
                MaterialStockDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Ширина (мм)",
                    Binding = new System.Windows.Data.Binding("WidthMm"),
                    Width = 100
                });
                MaterialStockDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Площадь (кв.м)",
                    Binding = new System.Windows.Data.Binding("AreaSqm"),
                    Width = 120
                });
                MaterialStockDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Погонные метры",
                    Binding = new System.Windows.Data.Binding("LengthM"),
                    Width = 120
                });
                MaterialStockDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Цена за м",
                    Binding = new System.Windows.Data.Binding("Price") { StringFormat = "C" },
                    Width = 100
                });
                MaterialStockDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Общая стоимость",
                    Binding = new System.Windows.Data.Binding("TotalCost") { StringFormat = "C" },
                    Width = 120
                });

                MaterialStockDataGrid.ItemsSource = stocks;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки остатков тканей: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadFittingStock()
        {
            try
            {
                if (_materialService == null)
                {
                    InitializeServices();
                }

                var stocks = await _materialService.GetFittingStockWithUnitsAsync();

                MaterialStockDataGrid.Columns.Clear();
                MaterialStockDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Партия",
                    Binding = new System.Windows.Data.Binding("BatchId"),
                    Width = 80
                });
                MaterialStockDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Артикул",
                    Binding = new System.Windows.Data.Binding("FittingArticle"),
                    Width = 120
                });
                MaterialStockDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Название",
                    Binding = new System.Windows.Data.Binding("FittingName"),
                    Width = 150
                });
                MaterialStockDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Количество (шт)",
                    Binding = new System.Windows.Data.Binding("Quantity"),
                    Width = 120
                });
                MaterialStockDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Вес единицы",
                    Binding = new System.Windows.Data.Binding("WeightValue"),
                    Width = 100
                });
                MaterialStockDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Общий вес",
                    Binding = new System.Windows.Data.Binding("TotalWeight"),
                    Width = 100
                });
                MaterialStockDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Цена за шт",
                    Binding = new System.Windows.Data.Binding("Price") { StringFormat = "C" },
                    Width = 100
                });
                MaterialStockDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Общая стоимость",
                    Binding = new System.Windows.Data.Binding("TotalCost") { StringFormat = "C" },
                    Width = 120
                });

                MaterialStockDataGrid.ItemsSource = stocks;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки остатков фурнитуры: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
