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

            // Показываем окно загрузки
            var loadingWindow = new Window
            {
                Title = "Инициализация базы данных...",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Content = new System.Windows.Controls.TextBlock
                {
                    Text = "Подождите, идет инициализация базы данных...",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 16
                }
            };
            loadingWindow.Show();

            try
            {
                var databaseService = new DatabaseService();
                await databaseService.InitializeDatabaseAsync();

                // Закрываем окно загрузки
                loadingWindow.Close();

                // Создаем и показываем главное окно
                var mainWindow = new MainWindow();
                MainWindow = mainWindow;
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                loadingWindow.Close();
                MessageBox.Show($"Ошибка инициализации базы данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

    }
}