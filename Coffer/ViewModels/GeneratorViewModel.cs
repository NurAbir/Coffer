using System.Windows;
using System.Windows.Input;
using Coffer.Services;

namespace Coffer.ViewModels;

public class GeneratorViewModel : BaseViewModel
{
    private readonly PasswordGenerator _generator;

    private int _length = 20;
    private bool _useUppercase = true;
    private bool _useLowercase = true;
    private bool _useDigits = true;
    private bool _useSymbols = true;
    private bool _excludeAmbiguous = true;
    private bool _isPassphraseMode;
    private int _wordCount = 6;
    private string _generatedPassword = string.Empty;
    private double _entropy;
    private string _strengthText = string.Empty;
    private int _strengthPercent;
    private string _copyButtonText = "Copy";
    private string _statusMessage = string.Empty;

    public GeneratorViewModel()
    {
        _generator = new PasswordGenerator();
        Generate();
    }

    public int Length
    {
        get => _length;
        set
        {
            int clamped = Math.Clamp(value, 8, 128);
            if (SetProperty(ref _length, clamped)) Generate();
        }
    }

    public bool UseUppercase
    {
        get => _useUppercase;
        set { if (SetProperty(ref _useUppercase, value)) Generate(); }
    }

    public bool UseLowercase
    {
        get => _useLowercase;
        set { if (SetProperty(ref _useLowercase, value)) Generate(); }
    }

    public bool UseDigits
    {
        get => _useDigits;
        set { if (SetProperty(ref _useDigits, value)) Generate(); }
    }

    public bool UseSymbols
    {
        get => _useSymbols;
        set { if (SetProperty(ref _useSymbols, value)) Generate(); }
    }

    public bool ExcludeAmbiguous
    {
        get => _excludeAmbiguous;
        set { if (SetProperty(ref _excludeAmbiguous, value)) Generate(); }
    }

    public bool IsPassphraseMode
    {
        get => _isPassphraseMode;
        set { if (SetProperty(ref _isPassphraseMode, value)) Generate(); }
    }

    public int WordCount
    {
        get => _wordCount;
        set
        {
            int clamped = Math.Clamp(value, 3, 12);
            if (SetProperty(ref _wordCount, clamped)) Generate();
        }
    }

    public string GeneratedPassword
    {
        get => _generatedPassword;
        private set => SetProperty(ref _generatedPassword, value);
    }

    public double Entropy
    {
        get => _entropy;
        private set => SetProperty(ref _entropy, value);
    }

    public string StrengthText
    {
        get => _strengthText;
        private set => SetProperty(ref _strengthText, value);
    }

    public int StrengthPercent
    {
        get => _strengthPercent;
        private set => SetProperty(ref _strengthPercent, value);
    }

    public string CopyButtonText
    {
        get => _copyButtonText;
        private set => SetProperty(ref _copyButtonText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public ICommand GenerateCommand => new RelayCommand(_ => Generate());
    public ICommand CopyCommand => new RelayCommand(_ => CopyToClipboard());

    private void Generate()
    {
        try
        {
            if (IsPassphraseMode)
            {
                GeneratedPassword = _generator.GeneratePassphrase(WordCount);
                Entropy = _generator.CalculatePassphraseEntropy(WordCount);
                StatusMessage = $"A {WordCount}-word passphrase. Words are easier to remember and type.";
            }
            else
            {
                GeneratedPassword = _generator.Generate(
                    Length,
                    UseUppercase,
                    UseLowercase,
                    UseDigits,
                    UseSymbols,
                    ExcludeAmbiguous);

                int poolSize = _generator.CharacterPoolSize(
                    UseUppercase,
                    UseLowercase,
                    UseDigits,
                    UseSymbols,
                    ExcludeAmbiguous);

                Entropy = _generator.CalculateEntropy(Length, poolSize);
                StatusMessage = "Generated locally with the Windows cryptographic random number generator.";
            }

            StrengthText = PasswordGenerator.StrengthLabel(Entropy);
            StrengthPercent = (int)Math.Clamp(Math.Round(Entropy), 0, 100);
        }
        catch (Exception ex)
        {
            GeneratedPassword = string.Empty;
            Entropy = 0;
            StrengthText = "Unavailable";
            StrengthPercent = 0;
            StatusMessage = ex.Message;
        }
    }

    private void CopyToClipboard()
    {
        if (string.IsNullOrEmpty(GeneratedPassword))
        {
            StatusMessage = "There is nothing to copy yet.";
            return;
        }

        ClipboardService.CopySensitive(
            GeneratedPassword,
            TimeSpan.FromSeconds(30),
            () => Application.Current?.Dispatcher.Invoke(() => CopyButtonText = "Copy"));

        CopyButtonText = "Copied";

        _ = Task.Run(async () =>
        {
            await Task.Delay(2000);
            Application.Current?.Dispatcher.Invoke(() => CopyButtonText = "Copy");
        });
    }
}