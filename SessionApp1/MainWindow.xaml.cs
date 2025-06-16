using SessionApp1.Views;
using System.Windows;

namespace SessionApp1
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex}");
                throw; // Перебрасываем исключение для остановки отладчика
            }
        }



        public void NavigateToPage(object page)
        {
            MainFrame.Navigate(page);
        }
    }
}