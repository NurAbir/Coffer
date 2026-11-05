using System.Windows.Controls;
using Coffer.ViewModels;

namespace Coffer.Views;

public partial class GeneratorView : UserControl
{
    public GeneratorView()
    {
        InitializeComponent();
        DataContext = new GeneratorViewModel();
    }
}
