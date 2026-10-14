using Avalonia.Controls;
using Avalonia.Input;
using FlowLauncher.Linux.Plugin;
using FlowLauncher.Linux.ViewModels;

namespace FlowLauncher.Linux;

public partial class MainWindow : Window
{
    private MainViewModel? ViewModel => DataContext as MainViewModel;

    public MainWindow()
    {
        InitializeComponent();
        Deactivated += (_, _) =>
        {
            if (ViewModel?.Settings?.HideOnLostFocus == true)
                Hide();
        };
    }

    private async void OnQueryKeyDown(object? sender, KeyEventArgs e)
    {
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
                await ViewModel.ExecuteSelectedResultAsync(e.KeyModifiers.HasFlag(KeyModifiers.Shift));
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

    private async void OnResultDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (ViewModel != null)
            await ViewModel.ExecuteSelectedResultAsync(false);
    }
}
