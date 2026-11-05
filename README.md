<p align="center">
  <img src="Coffer/Assets/logo.png" width="180" alt="Coffer logo"/>
</p>

<h1 align="center">Coffer</h1>
<p align="center"><b>A secure, fully offline password vault for Windows.</b></p>
<p align="center">Built with .NET 8 and WPF — your passwords never leave your device.</p>

<p align="center">
  <img src="https://img.shields.io/badge/platform-Windows%2010%2F11-blue?style=flat-square" alt="Platform"/>
  <img src="https://img.shields.io/badge/.NET-8.0-purple?style=flat-square" alt=".NET 8"/>
  <img src="https://img.shields.io/badge/encryption-AES--256--GCM-green?style=flat-square" alt="AES-256-GCM"/>
  <img src="https://img.shields.io/badge/KDF-Argon2id-orange?style=flat-square" alt="Argon2id"/>
  <img src="https://img.shields.io/badge/offline-100%25-informational?style=flat-square" alt="Offline"/>
  <img src="https://img.shields.io/badge/license-MIT-brightgreen?style=flat-square" alt="MIT License"/>
  <img src="https://img.shields.io/badge/version-1.1.0-yellow?style=flat-square" alt="Version 1.1.0"/>
</p>

---

> [!WARNING]
> **Coffer has no back door.** If you forget your master password **and** lose your 12-word
> recovery phrase, your vault cannot be recovered — by you or anyone else. There is no reset,
> no support override, and no cloud copy. **Write your recovery phrase down and store it
> offline.** This is a security feature, not a limitation.

---

## Description

**Coffer** is a lightweight, fully offline password manager for Windows. It stores every
credential in a single encrypted vault file on your own machine. No cloud account, no
telemetry, no network requests — a *coffer* is a strongbox for things worth protecting, which
is exactly what this is.

It is a rebrand and modernisation of the original **HeavenlyLock** password manager. Existing
HeavenlyLock vaults are migrated automatically on first launch and remain readable.

---

## Features

| Feature | Description |
| --- | --- |
| **AES-256-GCM encryption** | The whole vault is encrypted, and every password is encrypted again with its own random nonce |
| **Argon2id key derivation** | Memory-hard KDF that resists brute-force and GPU attacks |
| **12-word recovery phrase** | A second, independently wrapped copy of the data key so you can recover access |
| **Lockout protection** | 10 failed attempts trigger a 10-hour lockout that persists across restarts |
| **Auto-lock** | Locks itself after a configurable period of inactivity (Never / 1 / 5 / 10 / 15 / 30 min) |
| **Password generator** | Random passwords or memorable passphrases, with a live entropy meter |
| **Clipboard auto-clear** | Copied passwords are wiped from the clipboard after 30 seconds, and only if unchanged |
| **Atomic saves** | Writes to a temp file, flushes, keeps the previous vault as `vault.coffer.bak`, then swaps in |
| **Instant search** | Filter entries by service, username, URL, notes, or tag as you type |
| **Fully offline** | Zero network requests, zero telemetry, zero cloud dependency |
| **Portable or installed** | Ship as a single self-contained `.exe`, or install with a Start Menu shortcut |

---

## Requirements

- **Windows 10 or 11** (x64)
- No runtime install needed — the portable and installer builds bundle .NET 8

Building from source additionally needs the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8).

---

## Quick Start

### 1. Get Coffer

**Option A — Portable (recommended)**
Download `Coffer-Portable.exe` from the [Releases](https://github.com/NurAbir/Coffer/releases/latest)
page, drop it anywhere, and run it. No installation needed.

**Option B — Installer**
Download `Coffer-Setup.exe` from the same page and run it. It installs for your Windows user
(no administrator rights needed) and adds a Start Menu shortcut. Windows may show a SmartScreen
prompt — see [Troubleshooting](#troubleshooting).

### 2. First launch

Coffer opens on a **Create your vault** form. Choose a strong master password (at least 8
characters — longer is better), type it twice, and click **Create vault**.

> [!IMPORTANT]
> Your master password is never stored anywhere. If you forget it, your recovery phrase is the
> only way back in.

### 3. Save your recovery phrase

After the vault is created, Coffer shows a **12-word recovery phrase exactly once**. Write it
down in order and keep it offline. Tick the box to confirm, then continue.

See the [User Manual](USER_MANUAL.md) for the full walkthrough.

### Upgrading from HeavenlyLock?

On first run, if Coffer finds an existing
`%LocalAppData%\HeavenlyLock\vault.heavenly`, it copies it to
`%LocalAppData%\Coffer\vault.coffer`. The original file is left untouched and the vault format
is unchanged, so your existing master password keeps working.

---

## Development Setup

```
# Clone the repository
git clone https://github.com/NurAbir/Coffer.git
cd Coffer

# Restore and build
dotnet restore Coffer/Coffer.csproj
dotnet build Coffer/Coffer.csproj -c Debug

# Run the app
dotnet run --project Coffer/Coffer.csproj
```

You can also open `Coffer/Coffer.csproj` directly in Visual Studio 2022, JetBrains Rider, or
VS Code with the C# Dev Kit.

---

## Building from Source

Double-click a build script, or call `publish.ps1` from a terminal:

| Double-click | What it builds | Output |
| --- | --- | --- |
| `build-portable.bat` | Portable single-file EXE | `dist\Coffer-Portable.exe` |
| `build-installer.bat` | Portable EXE, then the Inno Setup installer | `dist\Coffer-Setup.exe` (plus `Coffer-Portable.exe`) |

```powershell
# Portable only
powershell -ExecutionPolicy Bypass -File .\publish.ps1 -SkipInstaller

# Portable + installer
powershell -ExecutionPolicy Bypass -File .\publish.ps1

# Raw publish, without the helper script
dotnet publish Coffer/Coffer.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true `
  -o publish
```

Output lands in `dist\`:

| File | Description |
| --- | --- |
| `Coffer-Portable.exe` | Single self-contained executable — no install needed |
| `Coffer-Setup.exe` | Inno Setup installer (requires [Inno Setup 6](https://jrsoftware.org/isdl.php)) |

> [!NOTE]
> `build-installer.bat` needs [Inno Setup 6](https://jrsoftware.org/isdl.php) installed. If it is
> missing, the script builds the portable EXE, then tells you the installer step could not run.
> There is no unit-test project yet; the CI workflow builds and publishes both artifacts on every
> push and creates a GitHub Release on version tags.

---

## Project Structure

```
Coffer/
├── Coffer/                         # WPF application project
│   ├── Coffer.csproj
│   ├── App.xaml / App.xaml.cs       # Theme, resources, global crash logging
│   ├── MainWindow.xaml / .cs        # Hosts login and dashboard, idle/auto-lock timer
│   ├── Assets/
│   │   ├── logo.png                 # Brand mark (source)
│   │   └── logo.ico                 # Multi-size Windows icon
│   ├── Models/
│   │   ├── Vault.cs                 # Vault = metadata + entries
│   │   ├── VaultEntry.cs            # One credential (password stored as ciphertext)
│   │   └── VaultMetadata.cs         # KDF params and wrapped keys (plaintext header)
│   ├── Services/
│   │   ├── CryptoService.cs         # AES-256-GCM, HKDF entry sub-keys
│   │   ├── VaultService.cs          # Create/unlock/rekey/save, atomic writes, migration
│   │   ├── PasswordGenerator.cs     # Random passwords, passphrases, recovery phrases
│   │   ├── PasswordStrength.cs      # Entropy estimate and strength label
│   │   ├── ClipboardService.cs      # Copy with auto-clear and tracked clearing
│   │   └── SettingsService.cs       # Non-secret preferences (auto-lock)
│   ├── ViewModels/
│   │   ├── LoginViewModel.cs        # Unlock, create, recovery, lockout
│   │   ├── MainViewModel.cs         # Vault state, autosave, lock, settings
│   │   ├── VaultEntryViewModel.cs   # One editable entry, lazy password decrypt
│   │   ├── GeneratorViewModel.cs    # Password generator page
│   │   ├── BaseViewModel.cs         # INotifyPropertyChanged base
│   │   └── RelayCommand.cs          # ICommand helper
│   └── Views/
│       ├── LoginView.xaml           # Unlock / create / recovery screens
│       ├── DashboardView.xaml       # Sidebar, entry list, status bar
│       ├── EntryEditorView.xaml     # Entry details editor
│       ├── GeneratorView.xaml       # Password generator
│       └── SettingsView.xaml        # Password change, backups, delete vault
├── installer/
│   └── Coffer.iss                   # Inno Setup installer script
├── .github/
│   └── workflows/build.yml          # CI: build, publish, and release
├── publish.ps1                      # One-command build script
├── build-portable.bat               # Double-click: portable EXE
├── build-installer.bat              # Double-click: portable EXE + installer
├── README.md                        # This file
├── USER_MANUAL.md                   # Detailed user guide
├── ENCRYPTION.md                    # How encryption works (technical reference)
├── CHANGELOG.md                     # Version history
├── CONTRIBUTING.md                  # How to contribute
├── SECURITY.md                      # Security policy and threat model
└── LICENSE                          # MIT
```

---

## Data Storage

Everything is stored **locally**. Nothing leaves your machine.

|  |  |
| --- | --- |
| **Vault file** | `%LocalAppData%\Coffer\vault.coffer` — metadata header + encrypted entries |
| **Settings** | `%LocalAppData%\Coffer\settings.json` — auto-lock preference (not secret) |
| **Lockout state** | `%LocalAppData%\Coffer\lockout.json` — failed-attempt counter and lockout time |
| **Crash safety** | Saves are atomic; the previous vault is kept as `vault.coffer.bak` |
| **Crash log** | `%LocalAppData%\Coffer\crash.log` — written only if an unhandled error occurs |

**Backup:** use **Settings → Save encrypted backup**, or copy the whole
`%LocalAppData%\Coffer\` folder. **Restore:** copy it back to the same location before
launching Coffer.

---

## Security

- The vault payload is encrypted with **AES-256-GCM**. A random 256-bit data encryption key
  (DEK) is wrapped with a key derived from your master password using **Argon2id**
  (3 iterations, 64 MB, 4 lanes).
- Each password is encrypted a second time with its own random nonce, using an HKDF sub-key
  derived from the DEK and the entry's unique id, so ciphertexts can't be swapped between
  entries.
- The master password is **never stored**. Only derived keys are used, and they exist in memory
  only while the vault is open. Locking wipes the DEK.
- The recovery phrase is a second copy of the DEK wrapped with a key derived from the phrase
  itself. It is shown once at creation and after a recovery.
- Saves are atomic: temp file → flush to disk → copy previous to `.bak` → atomic move.

> [!CAUTION]
> Coffer is provided **as-is, without warranty** (see [LICENSE](LICENSE)). It has not been
> independently security-audited. Review the source before trusting it with your most
> sensitive credentials, and always keep an offline backup of both your vault and your
> recovery phrase.

See the [full encryption explainer](ENCRYPTION.md) for the key hierarchy, algorithms, and
parameters, and the [threat model and reporting process](SECURITY.md) for the security policy.

---

## Troubleshooting

| Issue | Solution |
| --- | --- |
| **App won't start** | Confirm you are on Windows 10/11 x64. If it's the portable build, add an antivirus exception — single-file .NET executables are sometimes flagged falsely |
| **Windows SmartScreen blocked the installer** | Normal for new unsigned software. Click **More info → Run anyway** |
| **Forgot the master password** | Use the recovery phrase on the login screen — see [User Manual §8](USER_MANUAL.md#8-recovering-your-vault) |
| **Lost both password and recovery phrase** | The data cannot be recovered. There is no back door |
| **Vault file looks corrupted** | Restore from a backup, or try `vault.coffer.bak` beside the vault — see [User Manual §10](USER_MANUAL.md#10-backing-up-your-vault) |
| **"Could not save your changes"** | Check that `%LocalAppData%\Coffer\` is writable and has free disk space; the vault stays unlocked so you can retry |
| **Antivirus flagged Coffer** | Single-file self-contained .NET apps unpack to a temp folder on first run, which some scanners flag. Verify by building from source |
| **Legacy vault not found** | Migration only checks the default `%LocalAppData%\HeavenlyLock\` location and only runs when no Coffer vault exists yet |

---

## Contributing

Contributions are welcome.

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-feature`
3. Make your changes and build: `dotnet build Coffer/Coffer.csproj -c Debug`
4. Commit with clear messages
5. Push and open a Pull Request

See [CONTRIBUTING.md](CONTRIBUTING.md) for details.

---

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history.

---

## License

[MIT](LICENSE) — free to use, modify, and distribute.

<p align="center"><i>Made with care for your privacy.</i></p>
