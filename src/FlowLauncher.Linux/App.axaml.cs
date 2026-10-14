using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FlowLauncher.Linux.Core;
using FlowLauncher.Linux.Core.Plugin;
using FlowLauncher.Linux.Infrastructure;
using FlowLauncher.Linux.Infrastructure.Hotkey;
using FlowLauncher.Linux.Infrastructure.Logger;
using FlowLauncher.Linux.Infrastructure.Storage;
using FlowLauncher.Linux.Plugin;
using FlowLauncher.Linux.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FlowLauncher.Linux;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;
    private MainWindow? _mainWindow;
    private IGlobalHotkeyService? _hotkeyService;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Setup DI
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Load settings
            var settings = _serviceProvider.GetRequiredService<UserSettings>();

            // Create main window
            var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
            _mainWindow = new MainWindow { DataContext = mainViewModel };

            // Wire up view model hide request
            mainViewModel.RequestHide += () => _mainWindow.Hide();

            // Wire up PublicAPI window and query events
            PublicAPIInstance.RequestShowMainWindow += () =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (_mainWindow == null) return;
                    _mainWindow.Show();
                    _mainWindow.Focus();
                });
            };

            PublicAPIInstance.RequestHideMainWindow += () =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    _mainWindow?.Hide();
                });
            };

            PublicAPIInstance.RequestChangeQuery += (query, requery) =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    mainViewModel.QueryText = query;
                });
            };

            // Setup hotkey
            _hotkeyService = _serviceProvider.GetRequiredService<IGlobalHotkeyService>();
            _hotkeyService.RegisterHotkey(settings.Hotkey, () =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (_mainWindow == null) return;
                    if (_mainWindow.IsVisible)
                    {
                        _mainWindow.Hide();
                    }
                    else
                    {
                        _mainWindow.Show();
                        _mainWindow.Focus();
                        mainViewModel.ClearQuery();
                    }
                });
            });

            // Initialize plugins
            var pluginManager = _serviceProvider.GetRequiredService<IPluginManager>();
            await pluginManager.InitializeAsync();

            // Start hidden
            desktop.MainWindow = _mainWindow;
            _mainWindow.Show();

            desktop.Exit += (_, _) => Cleanup();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Settings
        var settingsPath = Path.Combine(Constant.DataDirectory, "settings.json");
        var settingsStorage = new JsonStorage<UserSettings>(settingsPath);
        var settings = settingsStorage.Load();
        services.AddSingleton(settings);
        services.AddSingleton(settingsStorage);

        // Infrastructure
        services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();

        // Core
        services.AddSingleton<IPluginManager, PluginManager>();
        services.AddSingleton<IPublicAPI, PublicAPIInstance>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
    }

    private void Cleanup()
    {
        _hotkeyService?.Dispose();
    }
}
