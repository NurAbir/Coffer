using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;
using Coffer.Models;
using Coffer.Services;

namespace Coffer.ViewModels;

public class LoginViewModel : BaseViewModel, IDisposable
{
    private readonly VaultService _vaultService;
    private readonly string _lockoutPath;

    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _recoveryPhrase = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _statusIsError;
    private bool _isBusy;
    private bool _isCreatingVault;
    private bool _isRecoveryMode;

    private bool _showRecoveryPhrase;
    private bool _hasConfirmedPhrase;
    private string _copyPhraseButtonText = "Copy phrase";
    private readonly ObservableCollection<string> _recoveryWords = new();
    private string _recoveryPhraseText = string.Empty;
    private VaultSession? _recoveredSession;

    private bool _isPostRecoveryChangePassword;
    private string _newPasswordAfterRecovery = string.Empty;
    private string _confirmPasswordAfterRecovery = string.Empty;

    private bool _isLockedOut;
    private string _lockoutMessage = string.Empty;
    private int _failedAttempts;
    private DateTime? _lockoutUntil;
    private System.Threading.Timer? _lockoutTimer;

    private const int MaxAttempts = 10;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromHours(10);

    public LoginViewModel(VaultService vaultService)
    {
        _vaultService = vaultService;
        _lockoutPath = vaultService.LockoutPath;
        _isCreatingVault = !_vaultService.VaultExists();

        LoadLockoutState();

        if (_isLockedOut)
        {
            UpdateLockoutDisplay();
            StartLockoutCountdown();
        }
        else if (_isCreatingVault)
        {
            SetStatus("Create a master password to set up your vault.");
        }
        else if (_vaultService.MigrationHappened)
        {
            SetStatus("Your vault was moved from the old app. Unlock it with your existing password.");
        }
        else
        {
            SetStatus("Enter your master password to unlock.");
        }
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetProperty(ref _confirmPassword, value);
    }

    public string RecoveryPhrase
    {
        get => _recoveryPhrase;
        set => SetProperty(ref _recoveryPhrase, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool StatusIsError
    {
        get => _statusIsError;
        set => SetProperty(ref _statusIsError, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
                RelayCommand.RaiseCanExecuteChanged();
        }
    }

    public bool IsCreatingVault
    {
        get => _isCreatingVault;
        set
        {
            if (!SetProperty(ref _isCreatingVault, value)) return;
            OnPropertyChanged(nameof(FormTitle));
            OnPropertyChanged(nameof(FormSubtitle));
        }
    }

    public string FormTitle => IsCreatingVault ? "Create your vault" : "Welcome back";

    public string FormSubtitle => IsCreatingVault
        ? "Choose one strong master password. Everything else follows from it."
        : "Unlock your encrypted vault to continue.";

    public bool IsRecoveryMode
    {
        get => _isRecoveryMode;
        set
        {
            if (SetProperty(ref _isRecoveryMode, value))
            {
                RecoveryPhrase = string.Empty;
                SetStatus(value
                    ? "Enter the 12-word recovery phrase you saved when you created the vault."
                    : IsCreatingVault
                        ? "Create a master password to set up your vault."
                        : "Enter your master password to unlock.");
            }
        }
    }

    public bool ShowRecoveryPhrase
    {
        get => _showRecoveryPhrase;
        set => SetProperty(ref _showRecoveryPhrase, value);
    }

    public bool HasConfirmedPhrase
    {
        get => _hasConfirmedPhrase;
        set
        {
            if (SetProperty(ref _hasConfirmedPhrase, value))
                RelayCommand.RaiseCanExecuteChanged();
        }
    }

    public string CopyPhraseButtonText
    {
        get => _copyPhraseButtonText;
        set => SetProperty(ref _copyPhraseButtonText, value);
    }

    public ObservableCollection<string> RecoveryWords => _recoveryWords;

    public string RecoveryPhraseText
    {
        get => _recoveryPhraseText;
        private set => SetProperty(ref _recoveryPhraseText, value);
    }

    public bool IsPostRecoveryChangePassword
    {
        get => _isPostRecoveryChangePassword;
        set => SetProperty(ref _isPostRecoveryChangePassword, value);
    }

    public string NewPasswordAfterRecovery
    {
        get => _newPasswordAfterRecovery;
        set => SetProperty(ref _newPasswordAfterRecovery, value);
    }

    public string ConfirmPasswordAfterRecovery
    {
        get => _confirmPasswordAfterRecovery;
        set => SetProperty(ref _confirmPasswordAfterRecovery, value);
    }

    public bool IsLockedOut
    {
        get => _isLockedOut;
        set => SetProperty(ref _isLockedOut, value);
    }

    public string LockoutMessage
    {
        get => _lockoutMessage;
        set => SetProperty(ref _lockoutMessage, value);
    }

    public ICommand UnlockCommand => new RelayCommand(
        async _ => await UnlockAsync(),
        _ => !IsBusy && !IsLockedOut && !IsCreatingVault);

    public ICommand CreateVaultCommand => new RelayCommand(
        async _ => await CreateVaultAsync(),
        _ => !IsBusy && IsCreatingVault);

    public ICommand RecoveryLoginCommand => new RelayCommand(
        async _ => await RecoveryLoginAsync(),
        _ => !IsBusy && IsRecoveryMode);

    public ICommand ToggleRecoveryModeCommand => new RelayCommand(_ => IsRecoveryMode = !IsRecoveryMode);

    public ICommand BackToLoginCommand => new RelayCommand(_ => IsRecoveryMode = false);

    public ICommand ContinueAfterRecoveryCommand => new RelayCommand(
        _ => ContinueAfterRecovery(),
        _ => HasConfirmedPhrase);

    public ICommand CopyRecoveryPhraseCommand => new RelayCommand(_ => CopyRecoveryPhrase());

    public ICommand SetNewPasswordAfterRecoveryCommand => new RelayCommand(
        async _ => await SetNewPasswordAfterRecoveryAsync(),
        _ => !IsBusy);

    public event EventHandler<VaultUnlockedEventArgs>? VaultUnlocked;
    public event EventHandler? ClearInputsRequested;

    private async Task UnlockAsync()
    {
        if (IsBusy || IsLockedOut || string.IsNullOrEmpty(Password)) return;

        string password = Password;
        IsBusy = true;
        SetStatus("Deriving vault key...");

        try
        {
            var session = await Task.Run(() => _vaultService.Unlock(password));
            ResetFailedAttempts();
            Password = string.Empty;
            ClearInputsRequested?.Invoke(this, EventArgs.Empty);
            VaultUnlocked?.Invoke(this, new VaultUnlockedEventArgs(session));
        }
        catch (InvalidPasswordException)
        {
            Password = string.Empty;
            ClearInputsRequested?.Invoke(this, EventArgs.Empty);
            HandleFailedAttempt("Incorrect password.");
        }
        catch (Exception ex)
        {
            Password = string.Empty;
            ClearInputsRequested?.Invoke(this, EventArgs.Empty);
            SetStatus($"Could not open the vault: {ex.Message}", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateVaultAsync()
    {
        if (IsBusy) return;

        if (Password.Length < VaultService.MinimumPasswordLength)
        {
            SetStatus($"Use at least {VaultService.MinimumPasswordLength} characters.", isError: true);
            return;
        }

        if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
        {
            SetStatus("The passwords do not match.", isError: true);
            return;
        }

        string password = Password;
        IsBusy = true;
        SetStatus("Creating your encrypted vault...");

        try
        {
            string phrase = await Task.Run(() => _vaultService.CreateVault(password));
            PresentRecoveryPhrase(phrase);
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            ClearInputsRequested?.Invoke(this, EventArgs.Empty);
            SetStatus("Vault created. Save your recovery phrase before continuing.");
        }
        catch (Exception ex)
        {
            SetStatus($"Could not create the vault: {ex.Message}", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void PresentRecoveryPhrase(string phrase)
    {
        RecoveryPhraseText = phrase;
        RecoveryWords.Clear();
        foreach (string word in phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            RecoveryWords.Add(word);

        HasConfirmedPhrase = false;
        CopyPhraseButtonText = "Copy phrase";
        ShowRecoveryPhrase = true;
    }

    private void CopyRecoveryPhrase()
    {
        if (string.IsNullOrEmpty(RecoveryPhraseText)) return;

        ClipboardService.CopySensitive(RecoveryPhraseText, TimeSpan.FromSeconds(60));
        CopyPhraseButtonText = "Copied";

        _ = Task.Run(async () =>
        {
            await Task.Delay(2000);
            Application.Current?.Dispatcher.Invoke(() => CopyPhraseButtonText = "Copy phrase");
        });
    }

    private void ContinueAfterRecovery()
    {
        if (!HasConfirmedPhrase) return;

        ShowRecoveryPhrase = false;
        HasConfirmedPhrase = false;
        CopyPhraseButtonText = "Copy phrase";

        if (_recoveredSession is not null)
        {
            var session = _recoveredSession;
            _recoveredSession = null;

            try
            {
                var freshVault = _vaultService.OpenWithDek(session.Dek);
                ResetFailedAttempts();
                VaultUnlocked?.Invoke(this, new VaultUnlockedEventArgs(new VaultSession(freshVault, session.Dek)));
            }
            catch (Exception ex)
            {
                SetStatus($"Could not reopen the vault: {ex.Message}", isError: true);
            }
        }
        else
        {
            IsCreatingVault = false;
            IsRecoveryMode = false;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            ClearInputsRequested?.Invoke(this, EventArgs.Empty);
            SetStatus("Vault ready. Unlock it with your new master password.");
        }
    }

    private async Task RecoveryLoginAsync()
    {
        if (IsBusy) return;

        if (string.IsNullOrWhiteSpace(RecoveryPhrase))
        {
            SetStatus("Enter your recovery phrase first.", isError: true);
            return;
        }

        string phrase = RecoveryPhrase;
        IsBusy = true;
        SetStatus("Checking your recovery phrase...");

        try
        {
            var session = await Task.Run(() => _vaultService.RecoverVault(phrase));
            _recoveredSession = session;
            RecoveryPhrase = string.Empty;
            IsRecoveryMode = false;
            IsPostRecoveryChangePassword = true;
            SetStatus("Recovery verified. Choose a new master password.");
        }
        catch (InvalidRecoveryPhraseException)
        {
            RecoveryPhrase = string.Empty;
            SetStatus("That recovery phrase is not valid. Check the words and order.", isError: true);
        }
        catch (Exception ex)
        {
            SetStatus($"Recovery failed: {ex.Message}", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SetNewPasswordAfterRecoveryAsync()
    {
        if (IsBusy) return;

        if (_recoveredSession is null)
        {
            IsPostRecoveryChangePassword = false;
            SetStatus("The recovery session expired. Start again.", isError: true);
            return;
        }

        if (NewPasswordAfterRecovery.Length < VaultService.MinimumPasswordLength)
        {
            SetStatus($"Use at least {VaultService.MinimumPasswordLength} characters.", isError: true);
            return;
        }

        if (!string.Equals(NewPasswordAfterRecovery, ConfirmPasswordAfterRecovery, StringComparison.Ordinal))
        {
            SetStatus("The new passwords do not match.", isError: true);
            return;
        }

        string newPassword = NewPasswordAfterRecovery;
        byte[] dek = _recoveredSession.Dek;
        IsBusy = true;
        SetStatus("Re-encrypting your vault...");

        try
        {
            string newPhrase = await Task.Run(() => _vaultService.ResetMasterPasswordWithDek(dek, newPassword));

            NewPasswordAfterRecovery = string.Empty;
            ConfirmPasswordAfterRecovery = string.Empty;
            ClearInputsRequested?.Invoke(this, EventArgs.Empty);
            IsPostRecoveryChangePassword = false;
            PresentRecoveryPhrase(newPhrase);
            SetStatus("Password changed. Save your new recovery phrase.");
        }
        catch (Exception ex)
        {
            SetStatus($"Could not change the password: {ex.Message}", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void HandleFailedAttempt(string baseMessage)
    {
        _failedAttempts++;
        SaveLockoutState();

        int remaining = MaxAttempts - _failedAttempts;

        if (_failedAttempts >= MaxAttempts)
        {
            _lockoutUntil = DateTime.UtcNow.Add(LockoutDuration);
            IsLockedOut = true;
            SaveLockoutState();
            UpdateLockoutDisplay();
            StartLockoutCountdown();
        }
        else
        {
            string plural = remaining == 1 ? "attempt" : "attempts";
            SetStatus($"{baseMessage} {remaining} {plural} remaining before a {LockoutDuration.TotalHours:0}-hour lockout.", isError: true);
        }
    }

    private void ResetFailedAttempts()
    {
        _failedAttempts = 0;
        _lockoutUntil = null;
        SaveLockoutState();
    }

    private void UpdateLockoutDisplay()
    {
        if (_lockoutUntil.HasValue && DateTime.UtcNow < _lockoutUntil.Value)
        {
            TimeSpan remaining = _lockoutUntil.Value - DateTime.UtcNow;
            IsLockedOut = true;
            LockoutMessage = $"Try again in {FormatTimeSpan(remaining)}";
            SetStatus("Too many failed attempts.", isError: true);
        }
        else
        {
            IsLockedOut = false;
            _failedAttempts = 0;
            _lockoutUntil = null;
            SaveLockoutState();
            SetStatus(IsCreatingVault
                ? "Create a master password to set up your vault."
                : "Enter your master password to unlock.");
        }
    }

    private static string FormatTimeSpan(TimeSpan span)
    {
        if (span.TotalHours >= 1)
            return $"{(int)span.TotalHours}h {span.Minutes:D2}m {span.Seconds:D2}s";
        if (span.TotalMinutes >= 1)
            return $"{span.Minutes}m {span.Seconds:D2}s";
        return $"{(int)span.TotalSeconds}s";
    }

    private void StartLockoutCountdown()
    {
        _lockoutTimer?.Dispose();
        _lockoutTimer = new System.Threading.Timer(_ =>
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                UpdateLockoutDisplay();
                if (!IsLockedOut)
                {
                    _lockoutTimer?.Dispose();
                    _lockoutTimer = null;
                }
            });
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    private void LoadLockoutState()
    {
        try
        {
            if (!File.Exists(_lockoutPath)) return;

            var state = JsonSerializer.Deserialize<LockoutState>(File.ReadAllText(_lockoutPath));
            if (state is null) return;

            _failedAttempts = state.FailedAttempts;
            _lockoutUntil = state.LockoutUntil;

            if (_lockoutUntil.HasValue && DateTime.UtcNow < _lockoutUntil.Value)
            {
                _isLockedOut = true;
            }
            else if (_lockoutUntil.HasValue)
            {
                _failedAttempts = 0;
                _lockoutUntil = null;
                SaveLockoutState();
            }
        }
        catch
        {
            // A damaged lockout file should never block access to the app.
        }
    }

    private void SaveLockoutState()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_lockoutPath)!);
            var state = new LockoutState
            {
                FailedAttempts = _failedAttempts,
                LockoutUntil = _lockoutUntil
            };
            File.WriteAllText(_lockoutPath, JsonSerializer.Serialize(state));
        }
        catch
        {
            // Best effort.
        }
    }

    private void SetStatus(string message, bool isError = false)
    {
        StatusMessage = message;
        StatusIsError = isError;
    }

    public void Dispose()
    {
        _lockoutTimer?.Dispose();
        _lockoutTimer = null;
    }

    private class LockoutState
    {
        [JsonPropertyName("failedAttempts")]
        public int FailedAttempts { get; set; }

        [JsonPropertyName("lockoutUntil")]
        public DateTime? LockoutUntil { get; set; }
    }
}

public class VaultUnlockedEventArgs : EventArgs
{
    public VaultUnlockedEventArgs(VaultSession session)
    {
        Session = session;
    }

    public VaultSession Session { get; }
    public Vault Vault => Session.Vault;
    public byte[] Dek => Session.Dek;
}