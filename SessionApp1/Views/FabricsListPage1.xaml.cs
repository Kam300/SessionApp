using SessionApp1.Services;
using System;
using System.Windows.Controls;

namespace SessionApp1.Views
{
    public partial class FabricsListPage : Page
    {
        private readonly DatabaseService _databaseService;

        public FabricsListPage()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            LoadFabrics();
        }
        private async void LoadFabrics()
        {
            try
            {
                var fabrics = await _databaseService.GetFabricsAsync();
                FabricsDataGrid.ItemsSource = fabrics;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                    "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}