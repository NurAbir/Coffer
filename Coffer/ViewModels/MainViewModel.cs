using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Coffer.Models;
using Coffer.Services;
using Microsoft.Win32;

namespace Coffer.ViewModels;

public sealed record AutoLockOption(int Minutes, string Label)
{
    public override string ToString() => Label;
}

public class MainViewModel : BaseViewModel
{
    private readonly Vault _vault;
    private readonly byte[] _dek;
    private readonly VaultService _vaultService;
    private readonly CryptoService _cryptoService;
    private readonly SettingsService _settingsService;
    private readonly AppSettings _settings;
    private readonly ObservableCollection<VaultEntryViewModel> _entries = new();
    private readonly ICollectionView _entriesView;
    private readonly DispatcherTimer _saveTimer;

    private VaultEntryViewModel? _selectedEntry;
    private string _searchText = string.Empty;
    private string _statusMessage = "All changes saved";
    private bool _statusIsError;
    private bool _isDirty;
    private bool _isLocked;
    private int _selectedPageIndex;
    private AutoLockOption _selectedAutoLock;

    private bool _showDeleteEntryConfirm;
    private VaultEntryViewModel? _pendingDeleteEntry;
    private string _pendingDeleteEntryName = string.Empty;

    private bool _showDeleteVaultConfirm;
    private string _passwordChangeMessage = string.Empty;
    private bool _passwordChangeIsError;

    public MainViewModel(
        Vault vault,
        byte[] dek,
        VaultService vaultService,
        CryptoService cryptoService,
        SettingsService settingsService)
    {
        _vault = vault;
        _dek = dek;
        _vaultService = vaultService;
        _cryptoService = cryptoService;
        _settingsService = settingsService;
        _settings = settingsService.Load();

        AutoLockOptions = new[]
        {
            new AutoLockOption(0, "Never"),
            new AutoLockOption(1, "1 min"),
            new AutoLockOption(5, "5 min"),
            new AutoLockOption(10, "10 min"),
            new AutoLockOption(15, "15 min"),
            new AutoLockOption(30, "30 min")
        };

        _selectedAutoLock = AutoLockOptions.FirstOrDefault(o => o.Minutes == _settings.AutoLockMinutes)
                            ?? AutoLockOptions[2];

        foreach (var entry in _vault.Entries)
            AddEntryViewModel(entry);

        _entriesView = CollectionViewSource.GetDefaultView(_entries);
        _entriesView.Filter = FilterEntry;

        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(750) };
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            SaveNow();
        };

        SelectedEntry = _entries.FirstOrDefault();
        UpdateListEmpty();
    }

    public ObservableCollection<VaultEntryViewModel> Entries => _entries;

    public ICollectionView EntriesView => _entriesView;

    public bool IsListEmpty => _entriesView.IsEmpty;

    public string EntryCountText
    {
        get
        {
            int count = _entriesView.Cast<object>().Count();
            return count == 1 ? "1 entry" : $"{count} entries";
        }
    }

    public bool IsSearchEmpty => string.IsNullOrWhiteSpace(SearchText);

    public VaultEntryViewModel? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (SetProperty(ref _selectedEntry, value))
                RelayCommand.RaiseCanExecuteChanged();
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value)) return;

            _entriesView.Refresh();
            OnPropertyChanged(nameof(IsSearchEmpty));
            UpdateListEmpty();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool StatusIsError
    {
        get => _statusIsError;
        private set => SetProperty(ref _statusIsError, value);
    }

    public bool IsDirty
    {
        get => _isDirty;
        private set
        {
            if (SetProperty(ref _isDirty, value))
                OnPropertyChanged(nameof(DirtyStatusText));
        }
    }

    public string DirtyStatusText => IsDirty ? "Saving..." : "All changes saved";

    public int SelectedPageIndex
    {
        get => _selectedPageIndex;
        set
        {
            int clamped = Math.Clamp(value, 0, 2);
            if (!SetProperty(ref _selectedPageIndex, clamped)) return;

            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(PageSubtitle));
            OnPropertyChanged(nameof(IsVaultPage));
            OnPropertyChanged(nameof(IsGeneratorPage));
            OnPropertyChanged(nameof(IsSettingsPage));
        }
    }

    public bool IsVaultPage => SelectedPageIndex == 0;
    public bool IsGeneratorPage => SelectedPageIndex == 1;
    public bool IsSettingsPage => SelectedPageIndex == 2;

    public string PageTitle => SelectedPageIndex switch
    {
        1 => "Password generator",
        2 => "Settings",
        _ => "Vault"
    };

    public string PageSubtitle => SelectedPageIndex switch
    {
        1 => "Create strong, unique passwords locally.",
        2 => "Security, backups, and preferences.",
        _ => "Your encrypted passwords, stored only on this device."
    };

    public IReadOnlyList<AutoLockOption> AutoLockOptions { get; }

    public AutoLockOption SelectedAutoLock
    {
        get => _selectedAutoLock;
        set
        {
            if (!SetProperty(ref _selectedAutoLock, value)) return;

            _settings.AutoLockMinutes = value.Minutes;
            _settingsService.Save(_settings);
            OnPropertyChanged(nameof(AutoLockMinutes));
            OnPropertyChanged(nameof(AutoLockSummary));
        }
    }

    public int AutoLockMinutes => SelectedAutoLock.Minutes;

    public string AutoLockSummary => AutoLockMinutes == 0
        ? "Auto-lock is off"
        : $"Auto-lock after {AutoLockMinutes} min";

    public string VaultPath => _vaultService.VaultPath;

    public string DataDirectory => _vaultService.DataDirectory;

    public bool ShowDeleteEntryConfirm
    {
        get => _showDeleteEntryConfirm;
        private set => SetProperty(ref _showDeleteEntryConfirm, value);
    }

    public string PendingDeleteEntryName
    {
        get => _pendingDeleteEntryName;
        private set => SetProperty(ref _pendingDeleteEntryName, value);
    }

    public bool ShowDeleteVaultConfirm
    {
        get => _showDeleteVaultConfirm;
        private set => SetProperty(ref _showDeleteVaultConfirm, value);
    }

    private bool _deleteVaultConfirmed;
    public bool DeleteVaultConfirmed
    {
        get => _deleteVaultConfirmed;
        set
        {
            if (SetProperty(ref _deleteVaultConfirmed, value))
                RelayCommand.RaiseCanExecuteChanged();
        }
    }

    public string PasswordChangeMessage
    {
        get => _passwordChangeMessage;
        private set => SetProperty(ref _passwordChangeMessage, value);
    }

    public bool PasswordChangeIsError
    {
        get => _passwordChangeIsError;
        private set => SetProperty(ref _passwordChangeIsError, value);
    }

    public ICommand AddEntryCommand => new RelayCommand(_ => AddEntry());
    public ICommand SaveCommand => new RelayCommand(_ => SaveNow());
    public ICommand LockCommand => new RelayCommand(_ => Lock());
    public ICommand RequestDeleteEntryCommand => new RelayCommand(_ => RequestDeleteSelectedEntry(), _ => SelectedEntry is not null);
    public ICommand ConfirmDeleteEntryCommand => new RelayCommand(_ => ConfirmDeleteEntry());
    public ICommand CancelDeleteEntryCommand => new RelayCommand(_ => CancelDeleteEntry());
    public ICommand RequestDeleteVaultCommand => new RelayCommand(_ =>
    {
        DeleteVaultConfirmed = false;
        ShowDeleteVaultConfirm = true;
    });
    public ICommand ConfirmDeleteVaultCommand => new RelayCommand(_ => ConfirmDeleteVault(), _ => DeleteVaultConfirmed);
    public ICommand CancelDeleteVaultCommand => new RelayCommand(_ =>
    {
        DeleteVaultConfirmed = false;
        ShowDeleteVaultConfirm = false;
    });
    public ICommand BackupVaultCommand => new RelayCommand(_ => BackupVault());
    public ICommand OpenDataFolderCommand => new RelayCommand(_ => OpenDataFolder());

    public event EventHandler? RequestLock;
    public event EventHandler? RequestReset;

    public bool TryChangeMasterPassword(string currentPassword, string newPassword, string confirmPassword)
    {
        PasswordChangeMessage = string.Empty;
        PasswordChangeIsError = false;

        if (string.IsNullOrWhiteSpace(currentPassword))
        {
            SetPasswordChangeMessage("Enter your current password.", isError: true);
            return false;
        }

        if (newPassword.Length < VaultService.MinimumPasswordLength)
        {
            SetPasswordChangeMessage($"The new password must be at least {VaultService.MinimumPasswordLength} characters.", isError: true);
            return false;
        }

        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            SetPasswordChangeMessage("The new passwords do not match.", isError: true);
            return false;
        }

        // Flush pending entry edits before re-keying the vault.
        if (!SaveNow())
        {
            SetPasswordChangeMessage("Could not save pending changes, so the password was not changed.", isError: true);
            return false;
        }

        try
        {
            VaultMetadata metadata = _vaultService.ChangeMasterPassword(currentPassword, newPassword);
            _vault.Metadata = metadata;
            SetPasswordChangeMessage("Master password changed successfully.", isError: false);
            SetStatus("Master password changed.");
            return true;
        }
        catch (InvalidPasswordException)
        {
            SetPasswordChangeMessage("Your current password is incorrect.", isError: true);
            return false;
        }
        catch (ArgumentException ex)
        {
            SetPasswordChangeMessage(ex.Message, isError: true);
            return false;
        }
        catch (Exception ex)
        {
            SetPasswordChangeMessage($"Could not change the password: {ex.Message}", isError: true);
            return false;
        }
    }

    public bool SaveOnExit()
    {
        _saveTimer.Stop();

        if (!SaveNow())
            return false;

        ClearSensitiveState();
        return true;
    }

    public void Lock()
    {
        if (_isLocked) return;

        _saveTimer.Stop();

        if (!SaveNow())
        {
            SetStatus("Could not save your changes, so the vault was left unlocked. Check the data folder and try again.", isError: true);
            return;
        }

        ClearSensitiveState();
        RequestLock?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Wipes plaintext and the data key. Only call this after a successful save.
    /// </summary>
    public void ClearSensitiveState()
    {
        foreach (var entry in _entries)
            entry.ClearSensitive();

        ClipboardService.ClearTracked();
        _cryptoService.SecureClear(_dek);
        _isLocked = true;
    }

    private void AddEntry()
    {
        var entry = new VaultEntry
        {
            Service = "New entry",
            Username = string.Empty,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        _vault.Entries.Add(entry);
        var vm = AddEntryViewModel(entry);

        _entriesView.Refresh();
        SelectedPageIndex = 0;
        SelectedEntry = vm;
        UpdateListEmpty();

        MarkDirty();
        SetStatus("New entry added. Changes save automatically.");
    }

    private VaultEntryViewModel AddEntryViewModel(VaultEntry entry)
    {
        var vm = new VaultEntryViewModel(entry, _cryptoService, _dek, MarkDirty, SetStatus);
        vm.DeleteCommand = new RelayCommand(_ => RequestDeleteEntry(vm));
        _entries.Add(vm);
        return vm;
    }

    private void RequestDeleteSelectedEntry()
    {
        if (SelectedEntry is not null)
            RequestDeleteEntry(SelectedEntry);
    }

    private void RequestDeleteEntry(VaultEntryViewModel vm)
    {
        _pendingDeleteEntry = vm;
        PendingDeleteEntryName = string.IsNullOrWhiteSpace(vm.Service) ? "this entry" : vm.Service;
        ShowDeleteEntryConfirm = true;
    }

    private void ConfirmDeleteEntry()
    {
        if (_pendingDeleteEntry is null)
        {
            ShowDeleteEntryConfirm = false;
            return;
        }

        var vm = _pendingDeleteEntry;
        _pendingDeleteEntry = null;
        ShowDeleteEntryConfirm = false;

        _vault.Entries.Remove(vm.Entry);
        _entries.Remove(vm);
        _entriesView.Refresh();

        if (ReferenceEquals(SelectedEntry, vm))
            SelectedEntry = _entriesView.Cast<VaultEntryViewModel>().FirstOrDefault();

        UpdateListEmpty();
        MarkDirty();
        SaveNow();

        SetStatus($"Deleted \"{vm.Service}\".");
    }

    private void CancelDeleteEntry()
    {
        _pendingDeleteEntry = null;
        PendingDeleteEntryName = string.Empty;
        ShowDeleteEntryConfirm = false;
    }

    private void ConfirmDeleteVault()
    {
        ShowDeleteVaultConfirm = false;

        try
        {
            _saveTimer.Stop();
            _vaultService.DeleteVault();
            ClipboardService.ClearTracked();
            _cryptoService.SecureClear(_dek);
            _isLocked = true;
            RequestReset?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            SetStatus($"Could not delete the vault: {ex.Message}", isError: true);
        }
    }

    private void BackupVault()
    {
        if (!SaveNow())
        {
            SetStatus("Could not save pending changes, so the backup was not created.", isError: true);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Save an encrypted Coffer backup",
            Filter = "Coffer vault (*.coffer)|*.coffer|All files (*.*)|*.*",
            DefaultExt = ".coffer",
            AddExtension = true,
            FileName = $"coffer-backup-{DateTime.Now:yyyyMMdd-HHmm}.coffer"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            _vaultService.ExportBackup(dialog.FileName);
            SetStatus("Encrypted backup saved.");
        }
        catch (Exception ex)
        {
            SetStatus($"Backup failed: {ex.Message}", isError: true);
        }
    }

    private void OpenDataFolder()
    {
        try
        {
            Process.Start(new ProcessStartInfo(_vaultService.DataDirectory) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            SetStatus($"Could not open the data folder: {ex.Message}", isError: true);
        }
    }

    private void MarkDirty()
    {
        IsDirty = true;
        SetStatus("Unsaved changes");

        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private bool SaveNow(bool force = false)
    {
        if (!_isDirty && !force) return true;

        _saveTimer.Stop();

        try
        {
            foreach (var entry in _entries)
                entry.CommitPassword();

            _vaultService.SaveVault(_vault, _dek);

            foreach (var entry in _entries)
                entry.MarkPasswordSaved();

            IsDirty = false;
            SetStatus("All changes saved.");
            return true;
        }
        catch (Exception ex)
        {
            IsDirty = true;
            SetStatus($"Save failed: {ex.Message}", isError: true);
            return false;
        }
    }

    private bool FilterEntry(object item)
    {
        if (item is not VaultEntryViewModel vm) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        string query = SearchText.Trim();

        return Contains(vm.Service, query) ||
               Contains(vm.Username, query) ||
               Contains(vm.Url, query) ||
               Contains(vm.Notes, query) ||
               vm.Entry.Tags.Any(tag => Contains(tag, query));
    }

    private static bool Contains(string? value, string query)
        => value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;

    private void UpdateListEmpty()
    {
        OnPropertyChanged(nameof(IsListEmpty));
        OnPropertyChanged(nameof(EntryCountText));
    }

    private void SetStatus(string message) => SetStatus(message, isError: false);

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        StatusIsError = isError;
    }

    private void SetPasswordChangeMessage(string message, bool isError)
    {
        PasswordChangeMessage = message;
        PasswordChangeIsError = isError;
    }
}