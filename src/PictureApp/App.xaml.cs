using System;
using System.Windows;
using System.Windows.Threading;

namespace PictureApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

            string initialPath = e.Args != null && e.Args.Length > 0 ? e.Args[0] : null;

            var window = new MainWindow(initialPath);
            window.Show();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                "Произошла непредвиденная ошибка:\n\n" + e.Exception.Message,
                "PictureApp",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }

        private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            if (ex == null) return;
            MessageBox.Show(
                "Произошла непредвиденная ошибка:\n\n" + ex.Message,
                "PictureApp",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
