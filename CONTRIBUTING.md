# Contributing to Coffer

Thank you for your interest in contributing to Coffer — a fully offline password manager for
Windows.

## How to Contribute

### Reporting bugs

1. Check whether the issue already exists.
2. Open a new issue using the bug report template, including:
   - A clear title
   - Steps to reproduce
   - Expected vs. actual behaviour
   - Your Windows version and Coffer version (from **Settings → About**)
   - Any relevant log lines from `%LocalAppData%\Coffer\crash.log`

> **Never paste real passwords, recovery phrases, or vault files into an issue.** Redact them,
> or reproduce with a throwaway vault.

### Suggesting features

1. Open a GitHub Discussion or Issue.
2. Describe the feature and why it would be useful.
3. Mockups or examples are welcome.

### Code contributions

1. Fork the repository.
2. Create a feature branch: `git checkout -b feature/my-feature`.
3. Make your changes.
4. Build and run locally:
   ```
   dotnet build Coffer/Coffer.csproj -c Debug
   dotnet run --project Coffer/Coffer.csproj
   ```
5. Commit with clear messages.
6. Push and open a Pull Request.

## Development Setup

See [README.md](README.md#development-setup) for prerequisites and build commands. In short, you
need the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8); [Inno Setup 6](https://jrsoftware.org/isdl.php)
is only needed to build the installer.

## Project Layout

Coffer follows a small, conventional MVVM structure:

| Folder | Purpose |
| --- | --- |
| `Coffer/Models/` | Plain data types (`Vault`, `VaultEntry`, `VaultMetadata`) |
| `Coffer/Services/` | Crypto, vault I/O, generator, clipboard, settings — no UI dependencies |
| `Coffer/ViewModels/` | UI state and commands; the only place that mutates the vault |
| `Coffer/Views/` | XAML views and their minimal code-behind |
| `Coffer/Converters/` | Value converters for bindings |

## Architecture

- **`CryptoService`** owns all cryptography: AES-256-GCM, Argon2id, and HKDF entry sub-keys.
  It never touches the filesystem.
- **`VaultService`** owns the vault file: create, unlock, recover, rekey, save, export, migrate.
  It serialises the vault and writes it atomically. The on-disk metadata is authoritative.
- **`MainViewModel`** owns the unlocked session: entries, autosave, lock, and settings. It is the
  single writer of `_vault`.
- **`LoginViewModel`** owns unlock/create/recovery and the persisted lockout counter.

Please keep this separation: crypto and file I/O stay out of the views, and the views stay free
of business rules.

## Security rules

Because this is a password manager, security-sensitive changes get extra scrutiny. The full
design is documented in [ENCRYPTION.md](ENCRYPTION.md).

- Never persist the master password, the DEK, or plaintext secrets.
- Always wipe key material with `CryptographicOperations.ZeroMemory` when you are done with it.
- Never weaken the KDF parameters or fall back to a cheaper algorithm.
- New formats must remain backward compatible, or ship with an explicit migration.
- The app must make **no** network requests. Any dependency that phones home will be rejected.

If you are fixing a security issue, please follow [SECURITY.md](SECURITY.md) instead of opening a
public issue.

## Pull request guidelines

- Keep changes focused; one logical change per PR.
- Match the existing style (nullable reference types are enabled, implicit usings are on).
- Ensure `dotnet build Coffer/Coffer.csproj -c Debug` completes with **0 warnings, 0 errors**.
- Describe what changed and why, and mention any behaviour or format changes.
- If you change the vault format or the version number, update `Coffer.csproj`,
  `installer/Coffer.iss`, `CHANGELOG.md`, and the docs together.

## Questions?

Open a GitHub Discussion.
