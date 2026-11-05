using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace Coffer;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogCrash(e.Exception);
        MessageBox.Show(
            "Coffer hit an unexpected error. It has been written to the crash log in your Coffer data folder.\n\n" +
            e.Exception.Message,
            "Coffer",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
            LogCrash(exception);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogCrash(e.Exception);
        e.SetObserved();
    }

    private static void LogCrash(Exception exception)
    {
        try
        {
            string directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Coffer");

            Directory.CreateDirectory(directory);

            File.AppendAllText(
                Path.Combine(directory, "crash.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {exception}\n\n");
        }
        catch
        {
            // Logging must never make a crash worse.
        }
    }
}