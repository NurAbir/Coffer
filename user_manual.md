<p align="center">
  <img src="Coffer/Assets/logo.png" width="150" alt="Coffer logo"/>
</p>

<h1 align="center">Coffer — User Manual</h1>
<p align="center">Version 1.1.0</p>

---

> [!WARNING]
> **Read this first.** Coffer has no back door. If you forget your master password **and**
> lose your 12-word recovery phrase, your vault cannot be recovered. Nobody — not the
> developer, not Microsoft, not any recovery service — can open it. Write your recovery
> phrase down and store it offline, and keep a backup of your vault file.

---

## Table of Contents

1. [Installation](#1-installation)
2. [First Launch — Creating Your Vault](#2-first-launch--creating-your-vault)
3. [Unlocking Your Vault](#3-unlocking-your-vault)
4. [Managing Password Entries](#4-managing-password-entries)
5. [Using the Password Generator](#5-using-the-password-generator)
6. [Settings](#6-settings)
7. [Changing Your Master Password](#7-changing-your-master-password)
8. [Recovering Your Vault](#8-recovering-your-vault)
9. [Lockout Protection](#9-lockout-protection)
10. [Backing Up Your Vault](#10-backing-up-your-vault)
11. [Uninstalling](#11-uninstalling)
12. [Security Notes](#12-security-notes)
13. [Troubleshooting](#13-troubleshooting)

---

## 1. Installation

### Installer (recommended)

1. Download `Coffer-Setup.exe` from the [Releases page](https://github.com/NurAbir/Coffer/releases/latest).
2. Run the installer. Windows may show a SmartScreen prompt — click **More info** then
   **Run anyway** (normal for new unsigned software).
3. Follow the setup wizard. By default Coffer installs for your Windows user (no administrator
   rights needed), with a Start Menu shortcut.
4. A Start Menu shortcut is created automatically. An optional Desktop shortcut can be added
   during setup.
5. Launch Coffer from the Start Menu or Desktop.

### Portable

1. Download `Coffer-Portable.exe` from the [Releases page](https://github.com/NurAbir/Coffer/releases/latest).
2. Place it anywhere you like — a local folder, a USB drive, a network share.
3. Double-click to run. No installation required.

> **Note:** Both versions store vault data in the same location:
> `%LocalAppData%\Coffer\vault.coffer`
> so you can switch between installer and portable versions without losing your data.

---

## 2. First Launch — Creating Your Vault

The first time you open Coffer, no vault exists yet. The welcome screen becomes a **Create
vault** form.

### Step 1 — Choose a master password

Your master password is the single key that protects everything inside Coffer. Choose wisely:

- Minimum 8 characters (longer is strongly recommended)
- Use a mix of uppercase, lowercase, numbers, and symbols
- Do **not** reuse a password you use anywhere else
- Consider a long passphrase (e.g. `correct-horse-battery-staple`) — long and memorable beats
  short and complex

Type your chosen password into the **Master password** field, type it again in **Confirm master
password**, then click **Create vault**.

### Step 2 — Save your recovery phrase

After the vault is created, Coffer displays a **12-word recovery phrase**. This is the only way
to recover your vault if you forget your master password.

**Write it down on paper and store it somewhere safe — offline.**

- The words are shown in a three-column grid, in order.
- Click **Copy phrase** to copy it to your clipboard if you prefer, then paste it into a secure
  document.
- The phrase is shown **only once**. It cannot be retrieved later.
- Tick **I have written down all 12 words in order**, then click **Continue**.

> [!CAUTION]
> If you lose both your master password and your recovery phrase, your vault data cannot be
> recovered by anyone. There is no back door.

---

## 3. Unlocking Your Vault

Enter your master password and click **Unlock vault** (or press **Enter**).

If the password is correct, your vault opens and you land on the dashboard.

The first unlock may take a moment — Coffer deliberately runs **Argon2id** key derivation,
which is designed to be slow and memory-hard.

### Wrong password

If you enter the wrong password, Coffer shows how many attempts remain before lockout. See
[Section 9](#9-lockout-protection). Failed attempts clear the password box automatically.

### Locking the vault

Click **Lock vault** at the bottom of the sidebar at any time. Pending changes are saved first,
then the master password and decryption key are cleared from memory and you return to the login
screen.

The vault also locks automatically:

- after the **auto-lock** period configured in Settings (default 5 minutes of inactivity), and
- when you close the application.

> **Note:** If a save fails, Coffer will **not** lock. It shows a red status message instead, so
> you never silently lose the change you just typed.

---

## 4. Managing Password Entries

### Viewing entries

All your saved entries appear in the list on the left side of the **Vault** page. Click any
entry to view and edit its details on the right.

Use the **Search entries** box to filter by service name, username, website address, notes, or
tag. If nothing matches, the list says **No matches**.

### Adding an entry

1. Click **New entry** in the top-right.
2. A new entry called "New entry" is created and selected automatically.
3. Fill in the fields on the right:
   - **Service** — the name of the website or application (e.g. `GitHub`)
   - **Username or email**
   - **Password** — use the eye button to show it, the refresh button to generate one, and the
     copy button to copy it
   - **Website** — the address (optional); use the buttons to copy it or open it in your browser
   - **Tags** — comma-separated labels to help with organisation (e.g. `work, email`)
   - **Notes** — anything else worth remembering
4. Changes are saved **automatically** after a short pause. The status bar shows **Saving...**
   and then **All changes saved**.

> There is no Save button. Coffer saves as you type, and also when you lock, switch entries, or
> close the app. If a save ever fails, the status bar turns red and says so.

### Editing an entry

Click any entry in the list, then edit the fields on the right. The strength bar under the
password updates as you type.

### Copying a password

Select an entry and click the **copy** button next to the password. The password is copied to
your clipboard.

> The clipboard is automatically cleared after **30 seconds** — but only if nothing else has
> replaced its contents. Locking Coffer also clears any password it placed on the clipboard.

### Deleting an entry

Select an entry and click the **trash** button in the entry header. A confirmation dialog
appears. Deletion is permanent and saved immediately.

---

## 5. Using the Password Generator

Coffer includes a built-in password generator on the **Generator** page in the sidebar.

### Modes

Choose **Random password** or **Passphrase**.

| Mode | Options |
| --- | --- |
| **Random password** | Length (8–128), uppercase, lowercase, digits, symbols, and an option to exclude look-alike characters (`0 O 1 l I`) |
| **Passphrase** | Word count (3–12); words are joined with hyphens |

### Entropy display

The generator shows the estimated **entropy** in bits. Higher is better:

| Entropy | Strength |
| --- | --- |
| Below 40 bits | Weak — do not use |
| 40–60 bits | Fair |
| 60–80 bits | Good |
| 80–100 bits | Strong |
| Above 100 bits | Very strong |

### Generating and using a password

1. Configure the options.
2. The password regenerates automatically whenever you change a setting, or click **Generate new**.
3. Click **Copy** to copy it (the button briefly reads **Copied**).
4. Paste it into a new entry's **Password** field, or use the refresh button inside an entry.

---

## 6. Settings

The **Settings** page contains everything that is not tied to a single entry.

### Auto-lock

Choose how long Coffer waits after your last interaction before locking: **Never**, **1**, **5**,
**10**, **15**, or **30 minutes**. The current choice is also shown in the status bar. The
setting is stored outside the encrypted vault, so it applies before you unlock.

### Backups & data

- **Save encrypted backup** writes a copy of the encrypted vault to a location you choose. The
  backup is exactly as safe as the vault itself — it still needs your master password.
- **Open data folder** opens `%LocalAppData%\Coffer` in File Explorer.
- The **Vault location** field shows the full path to the current vault.

### Delete vault

Removes the vault and every saved password from this computer. You must tick a confirmation box
first. This cannot be undone.

---

## 7. Changing Your Master Password

You can change your master password at any time without losing entries.

1. Open **Settings**.
2. Under **Master password**, enter your **current master password**.
3. Enter and confirm your **new master password** (minimum 8 characters).
4. Click **Change master password**.

If successful, the vault is immediately re-encrypted with a fresh key and saved to disk. Your
entries are unaffected. Pending edits are flushed to disk first, so a failed save aborts the
change instead of losing data.

> **Note:** Changing your master password does **not** change your recovery phrase. The existing
> recovery phrase remains valid.

---

## 8. Recovering Your Vault

If you forget your master password, you can regain access using your 12-word recovery phrase.

1. On the login screen, click **Forgot your password? Use your recovery phrase**.
2. Enter your 12-word recovery phrase (words separated by spaces; capitalisation does not matter).
3. Click **Recover vault**.
4. If the phrase is correct, you will be asked to set a **new master password**.
5. Enter and confirm your new password, then click **Set password and generate phrase**.
6. A **new recovery phrase** is generated. Save it immediately — your old one is now invalid.
7. Click **Continue** to enter the vault.

> [!IMPORTANT]
> The recovery phrase is case-insensitive, but word order matters. Enter all 12 words in the
> correct order.

---

## 9. Lockout Protection

Coffer limits login attempts to protect against brute-force attacks.

- After **10 consecutive failed login attempts**, the vault locks for **10 hours**.
- The lockout countdown is shown on the login screen and continues even if you close and reopen
  the application.
- Once the lockout expires, you get another 10 attempts.
- A successful login at any point resets the failed attempt counter.

### Locked out and forgotten your password?

Use your recovery phrase — see [Section 8](#8-recovering-your-vault). Recovery is not affected
by the login lockout.

---

## 10. Backing Up Your Vault

Your vault is a single encrypted file located at:

```
%LocalAppData%\Coffer\vault.coffer
```

Coffer also keeps the previous version as `vault.coffer.bak` in case a save is interrupted.

### Recommended backup strategy

- Use **Settings → Save encrypted backup** regularly, and keep the file on an external drive,
  USB stick, or cloud storage.
- Store your recovery phrase separately from your vault backup (if someone gets both, they can
  recover your vault).
- After adding important new entries, make a fresh backup.

### Restoring a backup

1. Close Coffer if it is open.
2. Copy your backed-up vault file to `%LocalAppData%\Coffer\` and name it `vault.coffer`,
   replacing the existing file.
3. Open Coffer and unlock as normal.

> [!WARNING]
> Replacing `vault.coffer` discards whatever was in the current vault. Make sure your backup is
> the one you actually want before you overwrite.

---

## 11. Uninstalling

### Installer version

1. Open **Settings > Apps** (or **Control Panel > Programs**).
2. Find **Coffer** and click **Uninstall**.

### Portable version

Simply delete `Coffer-Portable.exe`.

### Removing vault data

Uninstalling the application does **not** delete your vault data. To completely remove everything:

1. Uninstall or delete the application as above.
2. Delete the folder `%LocalAppData%\Coffer\` manually.

> [!CAUTION]
> Deleting the vault folder permanently destroys all stored passwords. Make sure you no longer
> need them before doing this.

---

## 12. Security Notes

This section is a summary. For the full design — algorithms, parameters, key hierarchy, and
threat model — see [ENCRYPTION.md](ENCRYPTION.md) and [SECURITY.md](SECURITY.md).

### What is encrypted

- The entire vault, including all entry metadata (service names, usernames, URLs, notes, tags),
  is encrypted with **AES-256-GCM**.
- Each individual password is also encrypted separately with its own random nonce.
- The vault file stored on disk contains no plaintext passwords, usernames, or service names.

### How the master password works

Your master password is never stored anywhere. When you log in:

1. Your password is fed into **Argon2id** (a memory-hard key derivation function) along with a
   random salt to produce a key-encryption-key (KEK).
2. The KEK is used to unwrap the vault's data encryption key (DEK).
3. The DEK is used to decrypt the vault contents.
4. When you lock the vault, the DEK is wiped from memory.

This means even if someone obtains your vault file, they cannot read it without knowing your
master password.

### Recovery phrase security

The recovery phrase is a second encrypted copy of the DEK, protected by a key derived from the
phrase itself. It is stored inside the vault file. Treat it with the same care as your master
password.

### Clipboard security

Copied passwords are automatically cleared from the clipboard after 30 seconds, and only if the
clipboard still contains the same value. Locking the vault also clears any password Coffer
placed there. Still, avoid copying passwords on shared or untrusted computers.

### Limitations

Coffer protects data **at rest**. While the vault is unlocked, decrypted secrets exist in memory
and can be copied to the clipboard. It does not defend against malware already running on your
account, a compromised keyboard, or a screen recorder. Keep Windows updated and lock your PC
when you step away.

---

## 13. Troubleshooting

### "The application won't start"

- Confirm you are running Windows 10 or 11 (x64).
- If using the portable version, make sure your antivirus is not blocking the executable.
  Single-file .NET executables are sometimes flagged falsely — add an exception if needed.

### "I forgot my master password"

Use your recovery phrase — see [Section 8](#8-recovering-your-vault).

### "I lost both my master password and my recovery phrase"

Your vault data cannot be recovered. There is no back door, reset mechanism, or support override.
This is by design — it means no one else can access your data either.

### "The vault file seems corrupted"

Restore from a backup — see [Section 10](#10-backing-up-your-vault). Coffer keeps a
`vault.coffer.bak` beside the vault, which may also help. If you have no backup, the data cannot
be recovered.

### "Coffer says it could not save my changes"

The status bar turns red and the vault stays unlocked. Check that `%LocalAppData%\Coffer\` is
writable and that your disk is not full, then edit something (or lock/unlock) to retry. Closing
the app with unsaved changes prompts you first.

### "Windows SmartScreen is blocking the installer"

This is normal for new software that has not yet built up a reputation with Microsoft. Click
**More info** then **Run anyway**. The application does not connect to the internet and contains
no malicious code.

### "My antivirus flagged Coffer"

Single-file self-contained .NET executables are sometimes flagged as suspicious because they
unpack themselves to a temp folder on first run. This is normal behaviour for this build type.
You can verify the build yourself from the source code.

---

<p align="center">
  Coffer v1.1.0 — MIT License — see <a href="README.md">README</a>
</p>