using System.Diagnostics;
using System.Windows.Input;
using Coffer.Models;
using Coffer.Services;

namespace Coffer.ViewModels;

public class VaultEntryViewModel : BaseViewModel
{
    private readonly CryptoService _crypto;
    private readonly byte[] _dek;
    private readonly PasswordGenerator _generator;
    private readonly Action _changed;
    private readonly Action<string> _notify;

    private string _password = string.Empty;
    private bool _passwordLoaded;
    private bool _passwordDirty;
    private bool _passwordDecryptionFailed;
    private bool _isPasswordVisible;

    public VaultEntryViewModel(
        VaultEntry entry,
        CryptoService crypto,
        byte[] dek,
        Action changed,
        Action<string> notify)
    {
        Entry = entry;
        _crypto = crypto;
        _dek = dek;
        _changed = changed;
        _notify = notify;
        _generator = new PasswordGenerator();

        CopyPasswordCommand = new RelayCommand(_ => CopyPassword());
        CopyUsernameCommand = new RelayCommand(_ => CopyUsername());
        CopyUrlCommand = new RelayCommand(_ => CopyUrl(), _ => HasUrl);
        OpenUrlCommand = new RelayCommand(_ => OpenUrl(), _ => HasUrl);
        TogglePasswordVisibilityCommand = new RelayCommand(_ => IsPasswordVisible = !IsPasswordVisible);
        GeneratePasswordCommand = new RelayCommand(_ => GeneratePassword());
    }

    public VaultEntry Entry { get; }

    public ICommand CopyPasswordCommand { get; }
    public ICommand CopyUsernameCommand { get; }
    public ICommand CopyUrlCommand { get; }
    public ICommand OpenUrlCommand { get; }
    public ICommand TogglePasswordVisibilityCommand { get; }
    public ICommand GeneratePasswordCommand { get; }
    public ICommand? DeleteCommand { get; set; }

    public string Service
    {
        get => Entry.Service;
        set
        {
            if (Entry.Service == value) return;
            Entry.Service = value;
            Touch();
            OnPropertyChanged();
            OnPropertyChanged(nameof(ServiceInitial));
        }
    }

    public string ServiceInitial
    {
        get
        {
            string service = Service?.Trim() ?? string.Empty;
            return service.Length > 0 ? service[..1].ToUpperInvariant() : "\u2022";
        }
    }

    public string Username
    {
        get => Entry.Username;
        set
        {
            if (Entry.Username == value) return;
            Entry.Username = value;
            Touch();
            OnPropertyChanged();
        }
    }

    public string? Url
    {
        get => Entry.Url;
        set
        {
            if (Entry.Url == value) return;
            Entry.Url = value;
            Touch();
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUrl));
        }
    }

    public bool HasUrl => !string.IsNullOrWhiteSpace(Url);

    public string? Notes
    {
        get => Entry.Notes;
        set
        {
            if (Entry.Notes == value) return;
            Entry.Notes = value;
            Touch();
            OnPropertyChanged();
        }
    }

    public string TagsString
    {
        get => string.Join(", ", Entry.Tags);
        set
        {
            var tags = (value ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (Entry.Tags.SequenceEqual(tags)) return;

            Entry.Tags = tags;
            Touch();
            OnPropertyChanged();
            OnPropertyChanged(nameof(TagsDisplay));
        }
    }

    public string TagsDisplay => string.Join("  \u2022  ", Entry.Tags);

    public string Password
    {
        get
        {
            EnsurePasswordLoaded();
            return _password;
        }
        set
        {
            EnsurePasswordLoaded();
            if (_password == value) return;

            _password = value ?? string.Empty;
            _passwordLoaded = true;
            _passwordDirty = true;
            _passwordDecryptionFailed = false;

            Touch();
            OnPropertyChanged();
            OnPropertyChanged(nameof(PasswordStrengthText));
            OnPropertyChanged(nameof(PasswordStrengthPercent));
        }
    }

    public bool IsPasswordVisible
    {
        get => _isPasswordVisible;
        set
        {
            if (SetProperty(ref _isPasswordVisible, value))
                OnPropertyChanged(nameof(PasswordVisibilityGlyph));
        }
    }

    public string PasswordVisibilityGlyph => IsPasswordVisible ? "\uE8F4" : "\uE8F5";

    public string PasswordVisibilityText => IsPasswordVisible ? "Hide" : "Show";

    public string PasswordStrengthText
    {
        get
        {
            EnsurePasswordLoaded();
            return PasswordStrength.Evaluate(_password).Label;
        }
    }

    public int PasswordStrengthPercent
    {
        get
        {
            EnsurePasswordLoaded();
            return PasswordStrength.Evaluate(_password).Percent;
        }
    }

    public string ModifiedAtText => Entry.ModifiedAt.ToLocalTime().ToString("MMM d, yyyy \u00b7 h:mm tt");

    public string CreatedAtText => Entry.CreatedAt.ToLocalTime().ToString("MMM d, yyyy");

    public void CommitPassword()
    {
        if (!_passwordDirty) return;

        if (string.IsNullOrEmpty(_password))
        {
            Entry.EncryptedPassword = Array.Empty<byte>();
            Entry.PasswordNonce = Array.Empty<byte>();
            Entry.PasswordTag = Array.Empty<byte>();
        }
        else
        {
            var (cipherText, nonce, tag) = _crypto.EncryptPassword(_password, _dek, Entry.Id);
            Entry.EncryptedPassword = cipherText;
            Entry.PasswordNonce = nonce;
            Entry.PasswordTag = tag;
        }

        // _passwordDirty is cleared by MarkPasswordSaved() only after the vault
        // has actually been written to disk, so a failed save cannot silently
        // throw away the user's plaintext edit.
    }

    public void MarkPasswordSaved()
    {
        _passwordDirty = false;
    }

    public void ClearSensitive()
    {
        if (_passwordDirty) return; // never discard an unsaved password

        _password = string.Empty;
        _passwordLoaded = false;
    }

    private void EnsurePasswordLoaded()
    {
        if (_passwordLoaded) return;

        _passwordLoaded = true;

        if (Entry.EncryptedPassword is not { Length: > 0 })
        {
            _password = string.Empty;
            return;
        }

        try
        {
            _password = _crypto.DecryptPassword(
                Entry.EncryptedPassword,
                Entry.PasswordNonce ?? Array.Empty<byte>(),
                Entry.PasswordTag ?? Array.Empty<byte>(),
                _dek,
                Entry.Id);
            _passwordDirty = false;
        }
        catch
        {
            _password = string.Empty;
            _passwordDecryptionFailed = true;
            _notify("This password could not be decrypted. It will be left untouched unless you edit it.");
        }
    }

    private void CopyPassword()
    {
        EnsurePasswordLoaded();

        if (_passwordDecryptionFailed)
        {
            _notify("This password could not be decrypted.");
            return;
        }

        if (string.IsNullOrEmpty(_password))
        {
            _notify("There is no password to copy.");
            return;
        }

        ClipboardService.CopySensitive(_password, TimeSpan.FromSeconds(30), () => _notify("Clipboard cleared."));
        _notify("Password copied. The clipboard clears in 30 seconds.");
    }

    private void CopyUsername()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            _notify("There is no username to copy.");
            return;
        }

        ClipboardService.CopyText(Username);
        _notify("Username copied.");
    }

    private void CopyUrl()
    {
        if (!HasUrl) return;
        ClipboardService.CopyText(Url!);
        _notify("Website address copied.");
    }

    private void OpenUrl()
    {
        if (!HasUrl) return;

        if (!Uri.TryCreate(Url, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            _notify("This is not a valid http(s) address.");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch
        {
            _notify("Could not open the website.");
        }
    }

    private void GeneratePassword()
    {
        Password = _generator.Generate(20, true, true, true, true, true);
        IsPasswordVisible = true;
        _notify("A new 20-character password was generated. Remember to save.");
    }

    private void Touch()
    {
        Entry.ModifiedAt = DateTime.UtcNow;
        OnPropertyChanged(nameof(ModifiedAtText));
        _changed();
    }
}