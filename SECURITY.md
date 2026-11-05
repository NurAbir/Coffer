# Security Policy

Coffer is a fully offline password manager. Security is its entire purpose, so please read this
before using or contributing to it.

## Supported versions

| Version | Supported |
| --- | --- |
| 1.1.x | ✅ |
| < 1.1 | ❌ (HeavenlyLock lineage — please migrate) |

## Reporting a vulnerability

**Please do not open a public issue for security problems.**

Instead, report it privately using GitHub's
[private vulnerability reporting](https://github.com/NurAbir/Coffer/security/advisories/new)
(the **Security → Report a vulnerability** button on the repository).

Please include:

- A description of the issue and its impact
- Steps to reproduce (a proof of concept if possible)
- The Coffer version and your Windows version
- Any suggested fix

You can expect an acknowledgement within a few days. Please allow reasonable time for a fix
before public disclosure.

> **Never include real vault files, master passwords, or recovery phrases in a report.** Create a
> throwaway vault to demonstrate the issue.

## Threat model

### What Coffer protects against

- **Theft of the vault file.** The vault is encrypted with AES-256-GCM. Without your master
  password (or recovery phrase), the file is not readable.
- **Offline brute-force of the master password.** Keys are derived with Argon2id
  (3 iterations, 64 MB, 4 lanes), a deliberately slow, memory-hard function.
- **Casual clipboard snooping.** Passwords copied from Coffer are cleared from the clipboard
  after 30 seconds, and locked/tracked values are cleared when the vault locks.
- **Accidental data loss on crash.** Vault writes are atomic and a one-generation
  `vault.coffer.bak` is kept.

### What Coffer does **not** protect against

- **Malware or a compromised account.** While the vault is unlocked, secrets are decrypted in
  memory and can be read by anything running as you. Coffer is not a sandbox.
- **Keyloggers, screen capture, or shoulder surfing.**
- **Weak or reused master passwords.** Argon2id slows guessing but cannot fix a guessable password.
- **Physical access to an unlocked machine.**
- **Loss of both the master password and the recovery phrase.** There is no back door, reset, or
  support override by design.

## Design summary

For a full explanation of the algorithms, parameters, and key hierarchy, see
[ENCRYPTION.md](ENCRYPTION.md).

- **Vault container:** JSON with a plaintext metadata header and an encrypted `data` blob.
- **Confidentiality:** AES-256-GCM (96-bit nonce, 128-bit tag) for the vault payload.
- **Key hierarchy:** master password → Argon2id → KEK → unwraps the data encryption key (DEK).
  Each entry's password is encrypted with an HKDF-SHA256 sub-key derived from the DEK and the
  entry's unique id, so ciphertexts cannot be swapped between entries.
- **Recovery:** the recovery phrase wraps a second copy of the DEK, derived with its own salt.
- **Lockout:** 10 failed attempts trigger a persisted 10-hour lockout.
- **Memory hygiene:** the DEK is zeroed on lock, delete, and exit.
- **No network:** the application makes no outbound connections.

## Disclaimer

Coffer is provided **as-is, without warranty of any kind** (see [LICENSE](LICENSE)). It has not
been independently security-audited. Review the source before trusting it with your most
sensitive credentials, and always keep offline backups of both your vault and your recovery
phrase.
