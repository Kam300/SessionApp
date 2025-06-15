using SessionApp1.Services;
using System.Windows.Controls;

namespace SessionApp1.Views
{
    public partial class ProductsListPage : Page
    {
        private readonly DatabaseService _databaseService;

        public ProductsListPage()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            LoadProducts();
        }

        private async void LoadProducts()
        {
            try
            {
                var products = await _databaseService.GetManufacturedGoodsAsync();
                ProductsDataGrid.ItemsSource = products;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                    "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}