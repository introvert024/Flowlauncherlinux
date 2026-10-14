namespace FlowLauncher.Linux.Plugin;

public static class AllowedLanguage
{
    public const string CSharp = "csharp";
    public const string Python = "python";
    public const string Python3 = "python3";
    public const string JavaScript = "javascript";
    public const string TypeScript = "typescript";
    public const string Executable = "executable";

    public static bool IsValid(string? lang) => lang?.ToLower() is CSharp or Python or Python3 or JavaScript or TypeScript or Executable;
}
