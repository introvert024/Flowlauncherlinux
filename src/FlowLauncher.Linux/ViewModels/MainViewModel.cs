using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlowLauncher.Linux.Core;
using FlowLauncher.Linux.Core.Plugin;
using FlowLauncher.Linux.Infrastructure.Logger;
using FlowLauncher.Linux.Infrastructure.Storage;
using FlowLauncher.Linux.Plugin;

namespace FlowLauncher.Linux.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IPluginManager _pluginManager;
    private readonly UserSettings _settings;
    private CancellationTokenSource _queryCts = new();

    [ObservableProperty] private string _queryText = string.Empty;
    [ObservableProperty] private ObservableCollection<Result> _results = new();
    [ObservableProperty] private Result? _selectedResult;
    [ObservableProperty] private string _statusText = string.Empty;

    public event Action? RequestHide;

    public UserSettings Settings => _settings;
    public bool HasQuery => !string.IsNullOrEmpty(QueryText);
    public bool HasResults => Results.Count > 0;

    public MainViewModel(IPluginManager pluginManager, UserSettings settings)
    {
        _pluginManager = pluginManager;
        _settings = settings;
    }

    partial void OnQueryTextChanged(string value)
    {
        OnPropertyChanged(nameof(HasQuery));
        _ = QueryAsync(value);
    }

    partial void OnResultsChanged(ObservableCollection<Result> value)
    {
        OnPropertyChanged(nameof(HasResults));
        StatusText = value.Count > 0 ? $"{value.Count} result(s)" : string.Empty;
    }

    partial void OnSelectedResultChanged(Result? value)
    {
        // Could update preview here
    }

    private async Task QueryAsync(string query)
    {
        _queryCts.Cancel();
        _queryCts = new CancellationTokenSource();
        var token = _queryCts.Token;

        try
        {
            await Task.Delay(100, token); // Debounce
            if (string.IsNullOrWhiteSpace(query))
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Results.Clear();
                    SelectedResult = null;
                    OnPropertyChanged(nameof(HasResults));
                    StatusText = string.Empty;
                });
                return;
            }

            var allResults = await _pluginManager.QueryAsync(Query.Parse(query), token);
            if (token.IsCancellationRequested) return;

            var sorted = allResults.OrderByDescending(r => r.Score).Take(_settings.MaxResultsToShow).ToList();

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Results.Clear();
                foreach (var r in sorted)
                    Results.Add(r);
                SelectedResult = Results.FirstOrDefault();
                OnPropertyChanged(nameof(HasResults));
                StatusText = $"{Results.Count} result(s)";
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Log.Error(nameof(MainViewModel), "Query error", ex);
        }
    }

    public void SelectNextResult()
    {
        if (Results.Count == 0) return;
        var idx = SelectedResult == null ? 0 : Results.IndexOf(SelectedResult) + 1;
        SelectedResult = Results[idx % Results.Count];
    }

    public void SelectPreviousResult()
    {
        if (Results.Count == 0) return;
        var idx = SelectedResult == null ? Results.Count - 1 : Results.IndexOf(SelectedResult) - 1;
        SelectedResult = Results[(idx + Results.Count) % Results.Count];
    }

    public async Task ExecuteSelectedResultAsync(bool shiftPressed)
    {
        if (SelectedResult?.Action == null) return;
        var ctx = new ActionContext { ShiftPressed = shiftPressed };
        var hide = await SelectedResult.Action(ctx);
        if (hide)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                RequestHide?.Invoke();
                if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow?.Hide();
                }
            });
        }
    }

    public void AutoComplete()
    {
        if (SelectedResult?.AutoCompleteText != null)
            QueryText = SelectedResult.AutoCompleteText;
    }

    [RelayCommand]
    public void ClearQuery()
    {
        QueryText = string.Empty;
        Results.Clear();
        SelectedResult = null;
        OnPropertyChanged(nameof(HasResults));
        StatusText = string.Empty;
    }
}
