using Avalonia.Controls;
using Avalonia.Input;
using FlowLauncher.Linux.ViewModels;

namespace FlowLauncher.Linux;

/// <summary>
/// MainWindow code-behind. Handles keyboard navigation and window focus/hide behavior.
/// The actual UI layout is defined in Views/MainWindow.axaml.
/// </summary>
public partial class MainWindow : Window
{
    private MainViewModel? ViewModel => DataContext as MainViewModel;

    public MainWindow()
    {
        InitializeComponent();

        // Hide window when it loses focus (configurable)
        Deactivated += (_, _) =>
        {
            if (ViewModel?.Settings?.HideOnLostFocus == true)
                Hide();
        };
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (ViewModel == null) return;

        switch (e.Key)
        {
            case Key.Down:
                ViewModel.SelectNextResult();
                e.Handled = true;
                break;
            case Key.Up:
                ViewModel.SelectPreviousResult();
                e.Handled = true;
                break;
            case Key.Enter:
                _ = ViewModel.ExecuteSelectedResultAsync(e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                e.Handled = true;
                break;
            case Key.Escape:
                Hide();
                e.Handled = true;
                break;
            case Key.Tab:
                ViewModel.AutoComplete();
                e.Handled = true;
                break;
        }
    }
}
