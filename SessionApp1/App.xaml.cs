using SessionApp1.Services;
using System;
using System.Windows;

namespace SessionApp1
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                var databaseService = new DatabaseService();
                await databaseService.InitializeDatabaseAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации базы данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}