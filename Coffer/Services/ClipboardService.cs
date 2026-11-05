using System.Runtime.InteropServices;
using System.Windows;

namespace Coffer.Services;

/// <summary>
/// Clipboard helpers that only clear the clipboard when it still contains the
/// value Coffer put there. The original app blindly cleared the clipboard after
/// 30 seconds, which could wipe something the user copied afterwards.
/// </summary>
public static class ClipboardService
{
    private static string? _lastSensitive;
    private static int _generation;

    public static void CopyText(string text) => SetText(text);

    public static void CopySensitive(string text, TimeSpan clearAfter, Action? onCleared = null)
    {
        if (string.IsNullOrEmpty(text)) return;

        string token = text;
        int generation = ++_generation;
        _lastSensitive = token;
        SetText(token);

        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = clearAfter
        };

        timer.Tick += (_, _) =>
        {
            timer.Stop();

            // A newer copy (even of the same text) supersedes this timer.
            if (generation != _generation) return;

            try
            {
                if (Clipboard.ContainsText() && Clipboard.GetText() == token)
                    Clipboard.Clear();
            }
            catch
            {
                // The clipboard may be locked by another process; nothing safe to do.
            }

            _lastSensitive = null;
            onCleared?.Invoke();
        };

        timer.Start();
    }

    /// <summary>
    /// Clears a password that Coffer placed on the clipboard, but only if the
    /// clipboard still holds that same value. Called when the vault locks.
    /// </summary>
    public static void ClearTracked()
    {
        string? tracked = _lastSensitive;
        _lastSensitive = null;
        _generation++; // invalidate any pending auto-clear timers

        if (string.IsNullOrEmpty(tracked)) return;

        try
        {
            if (Clipboard.ContainsText() && Clipboard.GetText() == tracked)
                Clipboard.Clear();
        }
        catch
        {
            // Ignore clipboard access failures.
        }
    }

    public static void ClearSensitive(string expectedValue)
    {
        try
        {
            if (!string.IsNullOrEmpty(expectedValue) &&
                Clipboard.ContainsText() &&
                Clipboard.GetText() == expectedValue)
            {
                Clipboard.Clear();
            }
        }
        catch
        {
            // Ignore clipboard access failures.
        }
    }

    private static void SetText(string text)
    {
        try
        {
            Clipboard.SetText(text);
        }
        catch (COMException)
        {
            // Retry once via SetDataObject, which uses a different clipboard path.
            try
            {
                Clipboard.SetDataObject(text, true);
            }
            catch
            {
                // Ignore; the caller shows a status message.
            }
        }
    }
}