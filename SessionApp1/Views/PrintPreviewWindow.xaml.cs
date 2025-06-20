using System.Windows;
using System.Windows.Controls;

namespace SessionApp1.Views
{
    public partial class PrintPreviewWindow : Window
    {
        public PrintPreviewWindow(string title, UIElement content)
        {
            InitializeComponent();
            
            // Установка заголовка окна
            Title = title;
            
            // Добавление содержимого в контейнер для печати
            if (content != null)
            {
                PrintContentContainer.Children.Clear();
                PrintContentContainer.Children.Add(content);
            }
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создание диалога печати
                var printDialog = new System.Windows.Controls.PrintDialog();
                
                if (printDialog.ShowDialog() == true)
                {
                    // Печать содержимого
                    printDialog.PrintVisual(PrintContentContainer, Title);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка печати: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}