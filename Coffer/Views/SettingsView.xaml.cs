using System.Windows;
using System.Windows.Controls;
using Coffer.ViewModels;

namespace Coffer.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void ChangePassword_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel) return;

        bool changed = viewModel.TryChangeMasterPassword(
            CurrentPasswordBox.Password,
            NewPasswordBox.Password,
            ConfirmPasswordBox.Password);

        if (changed)
        {
            CurrentPasswordBox.Clear();
            NewPasswordBox.Clear();
            ConfirmPasswordBox.Clear();
        }
    }
}
