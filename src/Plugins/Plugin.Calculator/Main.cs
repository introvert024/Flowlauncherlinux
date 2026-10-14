using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using FlowLauncher.Linux.Plugin;
using org.mariuszgromada.math.mxparser;

namespace FlowLauncher.Linux.Plugins.Calculator;

public class Main : IPlugin
{
    private PluginInitContext _context = null!;
    private const string IconPath = "Images/calculator.png";

    private static readonly Regex MathCharsRegex = new(
        @"[\d\.\+\-\*\/\^\%\(\)\,\s]",
        RegexOptions.Compiled);

    private static readonly Regex MathFunctionsRegex = new(
        @"\b(sin|cos|tan|asin|acos|atan|sinh|cosh|tanh|sqrt|cbrt|root|abs|log|ln|log10|log2|exp|round|floor|ceil|min|max|pi|e|deg|rad)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public Task InitAsync(PluginInitContext context)
    {
        _context = context;
        License.iConfirmNonCommercialUse("Flow Launcher");
        return Task.CompletedTask;
    }

    public Task<List<Result>> QueryAsync(Query query, CancellationToken token)
    {
        var rawSearch = query.Search?.Trim();
        if (string.IsNullOrWhiteSpace(rawSearch))
        {
            return Task.FromResult(new List<Result>());
        }

        // Avoid triggering on path-like or command-like inputs
        if (rawSearch.StartsWith('/') || rawSearch.StartsWith('~') || rawSearch.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new List<Result>());
        }

        if (!IsLikelyMathExpression(rawSearch))
        {
            return Task.FromResult(new List<Result>());
        }

        try
        {
            var expression = new Expression(rawSearch);
            if (!expression.checkSyntax())
            {
                return Task.FromResult(new List<Result>());
            }

            var value = expression.calculate();
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return Task.FromResult(new List<Result>());
            }

            var formattedResult = FormatResult(value);
            var title = $"{rawSearch} = {formattedResult}";

            var result = new Result
            {
                Title = title,
                SubTitle = "Press Enter to copy to clipboard",
                IcoPath = IconPath,
                Score = 300,
                CopyText = true,
                AutoCompleteText = formattedResult,
                Action = async _ =>
                {
                    await CopyToClipboardAsync(formattedResult);
                    _context.API.ShowMsg("Calculator", $"Copied '{formattedResult}' to clipboard", IconPath);
                    return true;
                }
            };

            return Task.FromResult(new List<Result> { result });
        }
        catch
        {
            return Task.FromResult(new List<Result>());
        }
    }

    private static bool IsLikelyMathExpression(string input)
    {
        // Must contain at least one digit or constant (pi, e)
        bool hasDigits = input.Any(char.IsDigit) ||
                         input.Contains("pi", StringComparison.OrdinalIgnoreCase) ||
                         input.Contains("e", StringComparison.OrdinalIgnoreCase);

        if (!hasDigits)
        {
            return false;
        }

        // Must contain at least one operator, parenthesis, or math function
        bool hasOperator = input.Any(c => "+-*/^%()".Contains(c)) || MathFunctionsRegex.IsMatch(input);
        if (!hasOperator)
        {
            return false;
        }

        // Remove allowed functions and constants
        var cleaned = MathFunctionsRegex.Replace(input, string.Empty);

        // All remaining non-whitespace characters should be valid math characters
        foreach (char c in cleaned)
        {
            if (char.IsLetter(c))
            {
                return false;
            }
        }

        return true;
    }

    private static string FormatResult(double value)
    {
        if (double.IsInteger(value) && value >= long.MinValue && value <= long.MaxValue)
        {
            return ((long)value).ToString(CultureInfo.InvariantCulture);
        }

        return value.ToString("G14", CultureInfo.InvariantCulture);
    }

    private static async Task<bool> CopyToClipboardAsync(string text)
    {
        var isWayland = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"));
        var tools = isWayland
            ? new[] { ("wl-copy", ""), ("xclip", "-selection clipboard"), ("xsel", "-b -i") }
            : new[] { ("xclip", "-selection clipboard"), ("xsel", "-b -i"), ("wl-copy", "") };

        foreach (var (tool, args) in tools)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = tool,
                    Arguments = args,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    await proc.StandardInput.WriteAsync(text);
                    proc.StandardInput.Close();
                    await proc.WaitForExitAsync();
                    if (proc.ExitCode == 0)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Fall through to next clipboard tool
            }
        }

        return false;
    }
}
