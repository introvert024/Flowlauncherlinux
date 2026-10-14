using System;
using System.IO;
using Avalonia;
using FlowLauncher.Linux.Infrastructure;
using FlowLauncher.Linux.Infrastructure.Logger;
using FlowLauncher.Linux.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace FlowLauncher.Linux;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Initialize logging
        var logDir = Path.Combine(Constant.DataDirectory, Constant.Logs);
        Log.Init(logDir);
        Log.Info(nameof(Program), "Flow Launcher for Linux starting...");

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Error(nameof(Program), "Fatal startup error", ex);
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
