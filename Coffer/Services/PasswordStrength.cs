namespace Coffer.Services;

public readonly record struct PasswordStrengthResult(string Label, int Percent, double EntropyBits);

public static class PasswordStrength
{
    public static PasswordStrengthResult Evaluate(string? password)
    {
        if (string.IsNullOrEmpty(password))
            return new PasswordStrengthResult("No password", 0, 0);

        int pool = 0;
        bool lower = password.Any(char.IsLower);
        bool upper = password.Any(char.IsUpper);
        bool digit = password.Any(char.IsDigit);
        bool symbol = password.Any(c => !char.IsLetterOrDigit(c));

        if (lower) pool += 26;
        if (upper) pool += 26;
        if (digit) pool += 10;
        if (symbol) pool += 33;
        if (pool == 0) pool = 1;

        double entropy = password.Length * Math.Log2(pool);

        // Penalise obvious repetition a little.
        int distinct = password.Distinct().Count();
        if (distinct <= 2 && password.Length > 3)
            entropy *= 0.45;

        string label = PasswordGenerator.StrengthLabel(entropy);
        int percent = (int)Math.Clamp(Math.Round(entropy), 0, 100);
        return new PasswordStrengthResult(label, percent, entropy);
    }
}