using SessionApp1.Services;
using System.Windows.Controls;

namespace SessionApp1.Views
{
    public partial class FittingsListPage : Page
    {
        private readonly DatabaseService _databaseService;

        public FittingsListPage()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            LoadFittings();
        }

        private async void LoadFittings()
        {
            try
            {
                var fittings = await _databaseService.GetFittingsAsync();
                FittingsDataGrid.ItemsSource = fittings;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                    "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}