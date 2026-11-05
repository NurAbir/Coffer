<p align="center">
  <img src="Coffer/Assets/logo.png" width="120" alt="Coffer logo"/>
</p>

<h1 align="center">Coffer — How Encryption Works</h1>
<p align="center">Version 1.1.0 · Technical reference</p>

---

> [!NOTE]
> This document explains what Coffer uses, how the pieces fit together, and why those choices
> are sound. It describes the implementation in `Coffer/Services/CryptoService.cs` and
> `Coffer/Services/VaultService.cs`. It is **not** a substitute for an independent security
> audit — see [SECURITY.md](SECURITY.md).

---

## Table of Contents

1. [At a Glance](#1-at-a-glance)
2. [Tools and Libraries](#2-tools-and-libraries)
3. [Key Hierarchy](#3-key-hierarchy)
4. [Creating a Vault](#4-creating-a-vault)
5. [Unlocking a Vault](#5-unlocking-a-vault)
6. [How Each Password Is Protected](#6-how-each-password-is-protected)
7. [The Recovery Phrase](#7-the-recovery-phrase)
8. [Changing the Master Password](#8-changing-the-master-password)
9. [What Is Stored in Plaintext](#9-what-is-stored-in-plaintext)
10. [Why These Choices Are Good for Protection](#10-why-these-choices-are-good-for-protection)
11. [Limits and Non-Goals](#11-limits-and-non-goals)
12. [References](#12-references)

---

## 1. At a Glance

| Purpose | Algorithm | Parameters |
| --- | --- | --- |
| Encrypt the vault and every entry | **AES-256-GCM** | 256-bit key, 96-bit random nonce, 128-bit auth tag |
| Derive keys from the master password / recovery phrase | **Argon2id** | 3 iterations, 65,536 KiB (64 MiB), 4 lanes, 32-byte salt, 32-byte output |
| Derive a unique key for each entry | **HKDF-SHA256** | Salt = entry id, info = `heavenly-lock-entry`, 32-byte output |
| Generate keys, salts, and nonces | **OS CSPRNG** | `RandomNumberGenerator` |
| Detect tampering | **AES-GCM authentication tag** | 128-bit tag checked on every decryption |
| Serialize the vault | **System.Text.Json** | UTF-8 JSON, byte arrays as Base64 |
| Persist to disk | **Atomic write** | Temp file → flush → `.bak` → atomic move |

Coffer uses only standard, well-reviewed primitives. There is **no custom cipher** and no
home-grown cryptographic protocol.

---

## 2. Tools and Libraries

| Component | Where it comes from | Role |
| --- | --- | --- |
| **AES-256-GCM** | .NET BCL — `System.Security.Cryptography.AesGcm` | Authenticated encryption of the vault payload and each password |
| **Argon2id** | [Konscious.Security.Cryptography.Argon2](https://www.nuget.org/packages/Konscious.Security.Cryptography.Argon2) 1.3.0 | Memory-hard password-based key derivation |
| **HKDF-SHA256** | .NET BCL — `System.Security.Cryptography.HKDF` | Per-entry sub-key derivation |
| **CSPRNG** | .NET BCL — `RandomNumberGenerator` | Keys, salts, nonces, recovery words |
| **SHA-256** | .NET BCL | The hash inside HKDF |
| **JSON** | .NET BCL — `System.Text.Json` | Vault serialization |

Argon2id is specified in **RFC 9106** and was the winner of the **Password Hashing Competition**
(2015). AES-GCM is standardized by **NIST SP 800-38D** and **FIPS 197**. HKDF is specified in
**RFC 5869**.

---

## 3. Key Hierarchy

```
                    master password                12-word recovery phrase
                          │                                  │
                    Argon2id (salt A)                  Argon2id (salt B)
                          │                                  │
                    Master KEK                          Recovery KEK
                          │                                  │
                  AES-GCM unwrap                     AES-GCM unwrap
                          │                                  │
                          └──────────────┬───────────────────┘
                                         ▼
                              Data Encryption Key (DEK)
                                   (random, 256-bit)
                                         │
                    ┌────────────────────┴────────────────────┐
                    │                                         │
        HKDF-SHA256(DEK, entry id)               AES-256-GCM(DEK)
                    │                                         │
           per-entry sub-key                        encrypted vault payload
                    │                                         │
          AES-256-GCM(password)                     service, username, URL,
                    │                                  notes, tags, and the
              encrypted password                       per-entry ciphertexts
```

The **DEK** is the only key that can decrypt the vault. It is never derived from the master
password; instead, the master password derives a **KEK** (key-encryption key) that wraps the DEK.
That indirection is what lets you change the master password without touching every entry.

---

## 4. Creating a Vault

When you choose a master password and click **Create vault**:

1. A random 256-bit **DEK** is generated with the OS CSPRNG.
2. A random 32-byte **salt** is generated.
3. The master **KEK** is derived: `Argon2id(password, salt, 3 iterations, 64 MiB, 4 lanes) → 32 bytes`.
4. The DEK is wrapped: `AES-256-GCM(KEK).Encrypt(DEK)` with a fresh 96-bit nonce. The ciphertext,
   nonce, and 128-bit tag are stored in the vault header.
5. A random **12-word recovery phrase** is generated from Coffer's 2,624-word list.
6. A **separate** salt and Argon2id run derive the recovery KEK, which wraps a second copy of the
   same DEK. This is why the recovery phrase can open the vault without knowing the password.
7. The vault (metadata + entries) is serialized to UTF-8 JSON and encrypted:
   `AES-256-GCM(DEK).Encrypt(vaultJson)` with a fresh nonce.
8. The container `{ "metadata": …, "data": "<ciphertext>" }` is written atomically.
9. Every key byte and plaintext buffer that is no longer needed is zeroed with
   `CryptographicOperations.ZeroMemory`.

---

## 5. Unlocking a Vault

When you type your master password:

1. The container is read and its metadata is validated: supported version, exact key/nonce/tag
   sizes, and sane Argon2 bounds. This prevents a malicious file from demanding absurd work.
2. `KEK = Argon2id(password, metadata.salt, …)`.
3. Coffer tries `AES-256-GCM(KEK).Decrypt(encryptedDek, dekNonce, dekTag)`. If the 128-bit tag
   does not verify, the password is wrong (or the header was tampered with) and the app reports
   an invalid password.
4. The DEK decrypts the outer payload: `AES-256-GCM(DEK).Decrypt(data, vaultNonce, vaultTag)`.
5. The JSON is deserialized into the in-memory vault.
6. The KEK is zeroed. The DEK stays in memory only while the vault is unlocked.

Because GCM is an **authenticated** mode, a wrong key or a modified byte produces an
authentication failure rather than garbage plaintext. There is no padding oracle and no
"partially correct" decryption.

---

## 6. How Each Password Is Protected

Storing every password directly under the DEK would mean one key encrypts many messages. Coffer
adds a per-entry layer:

1. A sub-key is derived for the entry:
   ```
   subKey = HKDF-SHA256(
       ikm  = DEK,
       salt = UTF-8(entry.Id as "N" GUID, 32 hex chars),
       info = "heavenly-lock-entry",
       length = 32 bytes)
   ```
2. The password is encrypted: `AES-256-GCM(subKey).Encrypt(UTF8(password))`, with its own random
   nonce and tag.
3. That ciphertext, nonce, and tag live on the entry. The **whole vault** — including these
   per-entry ciphertexts and all metadata — is then encrypted again with the DEK.

So a password is protected by **two independent AES-GCM layers**. The inner layer binds the
ciphertext to the entry's id: copying an encrypted password from one entry to another does not
work, because the recipient entry derives a different sub-key and the tag check fails.

> **Compatibility note:** the HKDF `info` string is `heavenly-lock-entry`, retained from the
> original HeavenlyLock app. It is not secret; it is a domain-separation label. Keeping it
> unchanged is what lets existing vaults decrypt after the rebrand.

When a password is edited, a fresh nonce is generated on save. When the vault is saved, the outer
payload is also re-encrypted with a fresh nonce.

---

## 7. The Recovery Phrase

- The phrase is **12 words** chosen uniformly at random from a curated **2,624-word** subset of
  the EFF long word list. That is about **136 bits** of entropy if the words are independent —
  far beyond brute-force reach.
- It is normalized to lowercase with single spaces, so capitalization and extra whitespace do
  not matter; word **order** does.
- It derives its **own** KEK with its **own** salt, then wraps the same DEK.
- It is shown exactly once at vault creation (and once after a recovery). It is not stored in
  plaintext anywhere.
- Because it is a second wrapping of the DEK, losing the master password does not lose the data —
  and changing the master password does not invalidate the phrase.

> [!WARNING]
> Anyone who obtains **both** your vault file and your recovery phrase can decrypt everything.
> Store the phrase separately from your backups.

---

## 8. Changing the Master Password

Changing the master password does **not** re-encrypt every entry, and it does **not** change the
recovery phrase:

1. Coffer verifies the current password by unlocking.
2. A new random salt is generated and a new KEK is derived with Argon2id.
3. The **same DEK** is re-wrapped with the new KEK.
4. The vault payload is re-encrypted with the DEK and a fresh nonce.
5. The old KEK is zeroed.

Because the DEK is unchanged, all existing per-entry ciphertexts remain valid. The recovery
phrase still unwraps the same DEK, so it keeps working.

When you recover with the phrase, Coffer instead sets a new master password **and** generates a
new recovery phrase (new salt + new wrapped copy), which invalidates the old phrase.

---

## 9. What Is Stored in Plaintext

The vault file is JSON with a plaintext header, similar in spirit to a KeePass database header:

```json
{
  "metadata": {
    "version": 2,
    "salt": "…",
    "argon2Iterations": 3,
    "argon2MemoryKB": 65536,
    "argon2Parallelism": 4,
    "encryptedDek": "…",
    "dekNonce": "…",
    "dekTag": "…",
    "recoverySalt": "…",
    "recoveryArgon2Iterations": 3,
    "recoveryArgon2MemoryKB": 65536,
    "recoveryArgon2Parallelism": 4,
    "recoveryEncryptedDek": "…",
    "recoveryDekNonce": "…",
    "recoveryDekTag": "…",
    "vaultNonce": "…",
    "vaultTag": "…"
  },
  "data": "<Base64 AES-256-GCM ciphertext>"
}
```

**Plaintext (not secret):** the format version, KDF parameters, salts, and the wrapped (encrypted)
DEK copies. These are needed before the password is known.

**Encrypted:** every entry — service name, username, password, URL, notes, tags, timestamps — is
inside the `data` blob. The file contains no plaintext credentials or service names.

The header is not secret, but it **is** tamper-evident in practice: changing the salt, KDF
parameters, wrapped keys, nonce, or tag causes authentication to fail. An attacker cannot weaken
the parameters or swap a wrapped key without breaking decryption.

---

## 10. Why These Choices Are Good for Protection

### AES-256-GCM — confidentiality *and* integrity
- **256-bit keys** have no known practical attack; even a quantum computer would need
  impractically large resources against AES-256.
- **GCM** is an authenticated mode: it encrypts and produces a 128-bit tag that detects any
  modification. A tampered vault fails closed instead of decrypting to attacker-chosen data.
- It is **hardware-accelerated** (AES-NI) on modern CPUs, so strong encryption costs almost
  nothing in responsiveness.

### Unique random nonces
- A fresh 96-bit nonce is generated for every encryption (each entry save and each vault save).
- A personal vault performs a tiny number of encryptions compared with the safe limit for random
  96-bit GCM nonces, so nonce reuse is not a practical concern.

### Argon2id — expensive to guess, even on GPUs
- Argon2id is the **RFC 9106** standard and the Password Hashing Competition winner.
- It is **memory-hard**: each guess needs 64 MiB, which sharply limits how many guesses a GPU or
  ASIC can run in parallel.
- The **id** variant mixes data-independent and data-dependent memory access, which resists both
  side-channel attacks and time-memory trade-offs.
- The cost parameters (3 passes, 64 MiB, 4 lanes) are stored in the file, so future versions can
  raise them without breaking old vaults.

### Key wrapping instead of password-as-key
- The password never encrypts data directly. It derives a KEK that wraps a random DEK.
- This enables cheap, safe password changes and lets the recovery phrase wrap the same DEK
  independently.

### Per-entry sub-keys
- HKDF gives each entry its own key, derived one-way from the DEK.
- Ciphertexts are bound to their entry id, so they cannot be swapped between entries.
- Learning one sub-key does not reveal the DEK or any other entry's key.

### Independent recovery path
- The recovery phrase uses its own salt and its own Argon2id derivation.
- It is a second wrapping of the same DEK, not a weaker bypass.

### Crash-safe storage
- Saves go to a temp file, are flushed to disk, the previous vault is copied to
  `vault.coffer.bak`, and only then is the new file moved into place.
- A crash cannot leave a half-written vault, and the `.bak` gives you a one-generation rollback.

### Offline by design
- Coffer makes no network requests. There is no sync server, account, or telemetry endpoint to
  attack, and no cloud provider that can be compelled to hand over your data.

---

## 11. Limits and Non-Goals

Coffer protects your data **at rest**. It does not pretend to protect it in every situation:

- **Not independently audited.** The design uses standard primitives, but no third-party audit
  has been performed. Review the source before trusting it with critical secrets.
- **Malware and compromised accounts.** While the vault is unlocked, decrypted secrets exist in
  memory and on the clipboard. A keylogger, memory scraper, screen recorder, or remote-access
  tool running as you can capture them. Encryption at rest cannot stop that.
- **Weak master passwords.** Argon2id makes guessing expensive, but it cannot rescue a password
  like `password1`. Use a long, unique passphrase.
- **Clipboard history.** Coffer clears its clipboard entry after 30 seconds, but Windows
  Clipboard History or a third-party clipboard manager may keep a copy. Disable clipboard
  history on shared machines.
- **Managed memory.** .NET strings and buffers may be copied by the garbage collector before
  being collected. Coffer zeroes key buffers it owns, but it cannot guarantee that no copy of a
  secret ever lingers in process memory.
- **No hardware-backed keys.** There is no TPM binding, Windows Hello, or hardware token
  integration yet. The master password and recovery phrase are the only factors.
- **Physical access while unlocked.** Anyone at an unlocked session can read the vault.
- **Lost credentials.** If you lose both the master password and the recovery phrase, the data is
  unrecoverable. That is the deliberate cost of having no back door.

---

## 12. References

- **RFC 9106** — Argon2 Memory-Hard Function for Password Hashing and Proof-of-Work Applications
- **NIST SP 800-38D** — Recommendation for Block Cipher Modes of Operation: GCM and GMAC
- **FIPS 197** — Advanced Encryption Standard (AES)
- **RFC 5869** — HMAC-based Extract-and-Expand Key Derivation Function (HKDF)
- **OWASP Password Storage Cheat Sheet** — current guidance on password hashing
- **Password Hashing Competition** — <https://www.password-hashing.net/>

---

<p align="center">
  <a href="README.md">README</a> ·
  <a href="USER_MANUAL.md">User Manual</a> ·
  <a href="SECURITY.md">Security Policy</a>
</p>