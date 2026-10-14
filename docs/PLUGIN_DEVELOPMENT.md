# Developing Plugins for Flow Launcher Linux

Flow Launcher Linux maintains backward-compatibility with Flow Launcher's plugin architecture. You can write plugins using **C# (.NET 8)**, **Python 3**, **Node.js / JavaScript**, or any **executable binary**.

---

## 1. Plugin Directory Structure

Plugins are installed in:
- System: `/usr/lib/flow-launcher/Plugins/<PluginName>/`
- User: `~/.config/flow-launcher/Plugins/<PluginName>/`

Each plugin must include a `plugin.json` manifest:

```json
{
  "ID": "A1B2C3D4-E5F6-7890-ABCD-1234567890AB",
  "ActionKeyword": "mykey",
  "Name": "My Plugin",
  "Description": "Custom search plugin for Flow Launcher Linux",
  "Author": "Your Name",
  "Version": "1.0.0",
  "Language": "python",
  "Website": "https://example.com",
  "ExecuteFilePath": "main.py",
  "IcoPath": "icon.png"
}
```

---

## 2. Python Plugin Example

Python plugins interact with Flow Launcher via JSON-RPC over `stdin` and `stdout`.

### `main.py`
```python
import sys
import json

def query(search):
    return [
        {
            "Title": f"Echo: {search}",
            "SubTitle": "Press Enter to copy to clipboard",
            "IcoPath": "icon.png",
            "Score": 100,
            "Action": {
                "method": "copy",
                "parameters": [search]
            }
        }
    ]

def main():
    while True:
        line = sys.stdin.readline()
        if not line:
            break
        request = json.loads(line)
        method = request.get("method")
        req_id = request.get("id")

        if method == "initialize":
            sys.stdout.write(json.dumps({"id": req_id, "result": None}) + "\n")
            sys.stdout.flush()
        elif method == "query":
            params = request.get("params", {})
            search = params.get("search", "")
            results = query(search)
            sys.stdout.write(json.dumps({"id": req_id, "result": results}) + "\n")
            sys.stdout.flush()

if __name__ == "__main__":
    main()
```

---

## 3. C# (.NET 8) Plugin Example

C# plugins are loaded directly into an isolated `AssemblyLoadContext` without IPC overhead.

### `Main.cs`
```csharp
using FlowLauncher.Linux.Plugin;

namespace MyCustomPlugin;

public class Main : IPlugin
{
    private IPublicAPI _api = null!;

    public Task InitAsync(PluginInitContext context)
    {
        _api = context.API;
        return Task.CompletedTask;
    }

    public Task<List<Result>> QueryAsync(Query query, CancellationToken token)
    {
        var results = new List<Result>
        {
            new Result
            {
                Title = $"Search query: {query.Search}",
                SubTitle = "Click or press Enter",
                Score = 100,
                Action = ctx =>
                {
                    _api.ShowMsg("Hello from C# Plugin!", query.Search);
                    return ValueTask.FromResult(true); // Return true to hide window
                }
            }
        };

        return Task.FromResult(results);
    }
}
```

### `.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FlowLauncher.Linux.Plugin" Version="1.0.0" />
  </ItemGroup>
</Project>
```
