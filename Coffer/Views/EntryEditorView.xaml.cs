using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Coffer.ViewModels;

namespace Coffer.Views;

public partial class EntryEditorView : UserControl
{
    private VaultEntryViewModel? _viewModel;
    private bool _syncing;

    public EntryEditorView()
    {
        InitializeComponent();

        Loaded += (_, _) => AttachViewModel();
        DataContextChanged += (_, _) => AttachViewModel();
        Unloaded += (_, _) => DetachViewModel();
    }

    private void AttachViewModel()
    {
        var viewModel = DataContext as VaultEntryViewModel;

        if (!ReferenceEquals(_viewModel, viewModel))
        {
            DetachViewModel();
            _viewModel = viewModel;

            if (_viewModel is not null)
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        PushPasswordToControls();
    }

    private void DetachViewModel()
    {
        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = null;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VaultEntryViewModel.Password))
            PushPasswordToControls();
    }

    // PasswordBox cannot bind its Password directly, so it is synchronised
    // with the view model by hand. The guard prevents feedback loops.
    private void PasswordEntry_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_syncing || _viewModel is null) return;

        if (_viewModel.Password != PasswordEntry.Password)
            _viewModel.Password = PasswordEntry.Password;
    }

    private void PushPasswordToControls()
    {
        _syncing = true;
        try
        {
            string password = _viewModel?.Password ?? string.Empty;

            if (PasswordEntry.Password != password)
                PasswordEntry.Password = password;

            if (PasswordText.Text != password)
                PasswordText.Text = password;
        }
        finally
        {
            _syncing = false;
        }
    }
}
