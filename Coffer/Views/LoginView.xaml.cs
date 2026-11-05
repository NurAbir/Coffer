using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Coffer.ViewModels;

namespace Coffer.Views;

public partial class LoginView : UserControl
{
    private LoginViewModel? _viewModel;

    public LoginView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.ClearInputsRequested -= OnClearInputsRequested;
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = e.NewValue as LoginViewModel;

        if (_viewModel is not null)
        {
            _viewModel.ClearInputsRequested += OnClearInputsRequested;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        ClearInputs();
    }

    private void OnClearInputsRequested(object? sender, EventArgs e) => ClearInputs();

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LoginViewModel.ShowRecoveryPhrase) or nameof(LoginViewModel.IsPostRecoveryChangePassword))
            ClearInputs();
    }

    private void ClearInputs()
    {
        PasswordBox.Password = string.Empty;
        ConfirmPasswordBox.Password = string.Empty;
        NewPasswordBox.Password = string.Empty;
        ConfirmNewPasswordBox.Password = string.Empty;
        UpdateCapsLockHint();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
            vm.Password = PasswordBox.Password;

        UpdateCapsLockHint();
    }

    private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
            vm.ConfirmPassword = ConfirmPasswordBox.Password;
    }

    private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
            vm.NewPasswordAfterRecovery = NewPasswordBox.Password;
    }

    private void ConfirmNewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
            vm.ConfirmPasswordAfterRecovery = ConfirmNewPasswordBox.Password;
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e) => UpdateCapsLockHint();

    private void PasswordBox_KeyUp(object sender, KeyEventArgs e) => UpdateCapsLockHint();

    private void UpdateCapsLockHint()
    {
        bool on = Keyboard.IsKeyToggled(Key.CapsLock);
        CapsLockHint.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
    }
}