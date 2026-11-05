# Changelog

All notable changes to Coffer.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-11-05

The first release under the **Coffer** name. Coffer is a rebrand and modernisation of the
original HeavenlyLock password manager. Existing HeavenlyLock vaults are migrated automatically
and stay readable — the vault format and key derivation are unchanged.

### Added

- **New brand and theme.** Warm brass (`#E0B15C`) on ink/navy surfaces, a programmatically drawn
  keyhole logo, and a consistent set of shared styles and converters.
- **Settings page** with master-password change, auto-lock selection, encrypted backup export,
  a link to the data folder, and vault deletion with a confirmation step.
- **Password generator page** with random-password and passphrase modes, an option to exclude
  look-alike characters, and a live entropy/strength meter.
- **Instant search** across service, username, URL, notes, and tags.
- **Legacy migration.** On first run, an existing
  `%LocalAppData%\HeavenlyLock\vault.heavenly` is copied to `%LocalAppData%\Coffer\vault.coffer`
  (the original is never modified).
- **Double-click build scripts.** `build-portable.bat` produces the portable EXE;
  `build-installer.bat` produces the portable EXE and the Inno Setup installer.
- **Technical encryption documentation.** `ENCRYPTION.md` explains the algorithms, parameters,
  key hierarchy, and protection limits in detail.

### Changed

- **Data location** moved to `%LocalAppData%\Coffer\` (`vault.coffer`, `settings.json`,
  `lockout.json`).
- **Autosave.** Entries save automatically after a short pause instead of requiring a Save button.
- **Locking and exiting are save-safe.** Lock, exit, backup, and password-change flows abort or
  warn if a save fails, so pending edits are never silently discarded.
- **Clipboard clearing is tracked.** Coffer only clears the clipboard if it still holds the value
  Coffer put there, so it can't wipe something you copied afterwards.
- Documentation rebranded and expanded (README, user manual, changelog, contributing, security).

### Fixed

- Failed unlock attempts now clear the password boxes.
- Save failures surface as a red status message instead of being swallowed.
- Closing the window with unsaved changes prompts before exiting.

### Security

- AES-256-GCM for the vault payload and for each entry (HKDF sub-key per entry id).
- Argon2id (3 iterations, 64 MB, 4 lanes) for master-password and recovery-phrase key derivation.
- DEK wiped from memory on lock, and tracked clipboard contents cleared on lock and on vault delete.
- Atomic vault writes (temp file → flush → `.bak` → atomic move).

[1.1.0]: https://github.com/NurAbir/Coffer/releases/tag/v1.1.0
