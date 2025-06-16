using System.Windows;
using System.Windows.Threading;

public partial class App : Application
{

    protected override void OnStartup(StartupEventArgs e)
    {
        this.DispatcherUnhandledException += (sender, args) =>
        {
            MessageBox.Show($"Ошибка: {args.Exception.ToString()}");
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            MessageBox.Show($"Критическая ошибка: {args.ExceptionObject.ToString()}");
        };

        base.OnStartup(e);
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Ошибка приложения: {e.Exception.ToString()}");
            e.Handled = true; // Предотвращает закрытие приложения
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Критическая ошибка: {e.ExceptionObject.ToString()}");
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            MessageBox.Show($"Ошибка в задаче: {e.Exception.ToString()}");
            e.SetObserved();
        }
}


    
