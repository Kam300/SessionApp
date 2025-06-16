using SessionApp1.Views;
using System.Windows;

namespace SessionApp1
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Сразу загружаем страницу авторизации
            MainFrame.Navigate(new LoginPage());
        }

        public void NavigateToPage(object page)
        {
            MainFrame.Navigate(page);
        }
    }
}