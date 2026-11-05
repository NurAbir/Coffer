using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Coffer.Services;
using Coffer.ViewModels;
using Coffer.Views;

namespace Coffer;

public partial class MainWindow : Window
{
    private readonly CryptoService _cryptoService;
    private readonly PasswordGenerator _passwordGenerator;
    private readonly VaultService _vaultService;
    private readonly SettingsService _settingsService;
    private readonly DispatcherTimer _idleTimer;

    private LoginViewModel? _loginViewModel;
    private MainViewModel? _mainViewModel;
    private DateTime _lastActivity = DateTime.UtcNow;

    public MainWindow()
    {
        InitializeComponent();

        _cryptoService = new CryptoService();
        _passwordGenerator = new PasswordGenerator();
        _vaultService = new VaultService(_cryptoService, _passwordGenerator);
        _settingsService = new SettingsService(_vaultService.DataDirectory);

        _idleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _idleTimer.Tick += OnIdleTick;

        PreviewMouseMove += OnUserActivity;
        PreviewMouseDown += OnUserActivity;
        PreviewMouseWheel += OnUserActivity;
        PreviewKeyDown += OnUserActivity;

        SetupLoginView();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Match the native title bar to the dark theme on Windows 10/11.
        try
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            int dark = 1;
            DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int));

            int rounded = 2;
            DwmSetWindowAttribute(handle, 33, ref rounded, sizeof(int));
        }
        catch
        {
            // Older Windows builds may not support these attributes.
        }
    }

    private void SetupLoginView()
    {
        _loginViewModel?.Dispose();

        var loginVm = new LoginViewModel(_vaultService);
        loginVm.VaultUnlocked += OnVaultUnlocked;

        _loginViewModel = loginVm;
        LoginViewControl.DataContext = loginVm;
        LoginViewControl.Visibility = Visibility.Visible;
        DashboardViewControl.Visibility = Visibility.Collapsed;
    }

    private void OnVaultUnlocked(object? sender, VaultUnlockedEventArgs e)
    {
        _idleTimer.Stop();

        var mainVm = new MainViewModel(
            e.Vault,
            e.Dek,
            _vaultService,
            _cryptoService,
            _settingsService);

        mainVm.RequestLock += OnRequestLock;
        mainVm.RequestReset += OnRequestReset;

        _mainViewModel = mainVm;
        DashboardViewControl.DataContext = mainVm;
        LoginViewControl.Visibility = Visibility.Collapsed;
        DashboardViewControl.Visibility = Visibility.Visible;

        _lastActivity = DateTime.UtcNow;
        _idleTimer.Start();
    }

    private void OnRequestLock(object? sender, EventArgs e)
    {
        _idleTimer.Stop();
        _mainViewModel = null;
        DashboardViewControl.DataContext = null;
        DashboardViewControl.Visibility = Visibility.Collapsed;
        SetupLoginView();
    }

    private void OnRequestReset(object? sender, EventArgs e)
    {
        _idleTimer.Stop();
        _mainViewModel = null;
        DashboardViewControl.DataContext = null;
        DashboardViewControl.Visibility = Visibility.Collapsed;
        SetupLoginView();

        if (LoginViewControl.DataContext is LoginViewModel login)
        {
            login.StatusMessage = "Your vault has been deleted from this device.";
            login.StatusIsError = false;
        }
    }

    private void OnUserActivity(object sender, InputEventArgs e)
    {
        _lastActivity = DateTime.UtcNow;
    }

    private void OnIdleTick(object? sender, EventArgs e)
    {
        if (_mainViewModel is null) return;

        int minutes = _mainViewModel.AutoLockMinutes;
        if (minutes <= 0) return;

        if (DateTime.UtcNow - _lastActivity >= TimeSpan.FromMinutes(minutes))
            _mainViewModel.Lock();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        _idleTimer.Stop();

        if (_mainViewModel is not null && !_mainViewModel.SaveOnExit())
        {
            MessageBoxResult choice = MessageBox.Show(
                "Coffer could not save your latest changes. If you close now, those changes may be lost.\n\nClose anyway?",
                "Coffer",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (choice != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                _idleTimer.Start();
                return;
            }

            _mainViewModel.ClearSensitiveState();
        }

        base.OnClosing(e);
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}