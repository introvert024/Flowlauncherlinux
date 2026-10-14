using System.Diagnostics;
using System.Text.Json;
using FlowLauncher.Linux.Infrastructure.Logger;
using FlowLauncher.Linux.Plugin;

namespace FlowLauncher.Linux.Core.Plugin;

/// <summary>
/// Handles external plugins (Python, Node.js, Executable) via JSON-RPC over stdin/stdout.
/// Compatible with Flow Launcher's existing plugin protocol.
/// </summary>
public class JsonRpcPlugin : IPlugin, IDisposable
{
    private readonly PluginMetadata _metadata;
    private readonly string? _interpreter;
    private Process? _process;
    private StreamWriter? _stdin;
    private StreamReader? _stdout;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private int _requestId = 0;

    public JsonRpcPlugin(PluginMetadata metadata, string? interpreter)
    {
        _metadata = metadata;
        _interpreter = interpreter;
    }

    public async Task InitAsync(PluginInitContext context)
    {
        var executeFile = Path.Combine(_metadata.PluginDirectory, _metadata.ExecuteFilePath);
        string fileName, arguments;
        if (_interpreter != null)
        { fileName = _interpreter; arguments = $"\"{executeFile}\""; }
        else
        { fileName = executeFile; arguments = string.Empty; }

        var psi = new ProcessStartInfo
        {
            FileName = fileName, Arguments = arguments,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
            UseShellExecute = false, CreateNoWindow = true,
            WorkingDirectory = _metadata.PluginDirectory
        };

        try
        {
            _process = Process.Start(psi);
            if (_process == null) return;
            _stdin = _process.StandardInput;
            _stdout = _process.StandardOutput;

            // Send init request
            var initRequest = new { id = ++_requestId, method = "initialize", @params = new { currentPluginMetadata = context.Metadata } };
            await SendRequestAsync(initRequest);
        }
        catch (Exception ex)
        {
            Log.Error(nameof(JsonRpcPlugin), $"Failed to start plugin process: {_metadata.Name}", ex);
        }
    }

    public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
    {
        if (_process == null || _stdin == null || _stdout == null) return new();
        try
        {
            await _lock.WaitAsync(token);
            try
            {
                var request = new { id = ++_requestId, method = "query", @params = new { rawQuery = query.RawQuery, search = query.Search } };
                var json = JsonSerializer.Serialize(request);
                await _stdin.WriteLineAsync(json);
                await _stdin.FlushAsync(token);

                var response = await _stdout.ReadLineAsync(token);
                if (string.IsNullOrEmpty(response)) return new();

                using var doc = JsonDocument.Parse(response);
                if (!doc.RootElement.TryGetProperty("result", out var resultEl)) return new();

                var results = new List<Result>();
                foreach (var item in resultEl.EnumerateArray())
                {
                    results.Add(new Result
                    {
                        Title = item.TryGetProperty("Title", out var t) ? t.GetString() ?? "" : "",
                        SubTitle = item.TryGetProperty("SubTitle", out var s) ? s.GetString() ?? "" : "",
                        IcoPath = item.TryGetProperty("IcoPath", out var ico) ? ico.GetString() ?? "" : "",
                        Score = item.TryGetProperty("Score", out var sc) ? sc.GetInt32() : 0
                    });
                }
                return results;
            }
            finally { _lock.Release(); }
        }
        catch (OperationCanceledException) { return new(); }
        catch (Exception ex)
        {
            Log.Error(nameof(JsonRpcPlugin), $"Query error for {_metadata.Name}", ex);
            return new();
        }
    }

    private async Task SendRequestAsync(object request)
    {
        if (_stdin == null) return;
        var json = JsonSerializer.Serialize(request);
        await _stdin.WriteLineAsync(json);
        await _stdin.FlushAsync();
        // Read and discard response
        await _stdout!.ReadLineAsync();
    }

    public void Dispose()
    {
        try { _process?.Kill(); _process?.Dispose(); } catch { }
    }
}
