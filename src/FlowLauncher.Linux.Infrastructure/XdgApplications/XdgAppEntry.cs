using System;
using System.IO;
using System.Linq;

namespace FlowLauncher.Linux.Infrastructure.XdgApplications;

public class XdgAppEntry
{
    public string Name { get; set; } = string.Empty;
    public string GenericName { get; set; } = string.Empty;
    public string Exec { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Categories { get; set; } = string.Empty;
    public string Keywords { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public string TryExec { get; set; } = string.Empty;
    public bool Hidden { get; set; }
    public bool NoDisplay { get; set; }
    public string DesktopFilePath { get; set; } = string.Empty;

    public static XdgAppEntry? Parse(string filePath)
    {
        try
        {
            var lines = File.ReadAllLines(filePath);
            var entry = new XdgAppEntry { DesktopFilePath = filePath };
            bool inDesktopEntry = false;
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed == "[Desktop Entry]") { inDesktopEntry = true; continue; }
                if (trimmed.StartsWith('[') && trimmed != "[Desktop Entry]") { if (inDesktopEntry) break; continue; }
                if (!inDesktopEntry || !trimmed.Contains('=')) continue;
                var eqIdx = trimmed.IndexOf('=');
                var key = trimmed[..eqIdx].Trim();
                var value = trimmed[(eqIdx + 1)..].Trim();
                switch (key)
                {
                    case "Name": entry.Name = value; break;
                    case "GenericName": entry.GenericName = value; break;
                    case "Exec": entry.Exec = value; break;
                    case "Icon": entry.Icon = value; break;
                    case "Categories": entry.Categories = value; break;
                    case "Keywords": entry.Keywords = value; break;
                    case "Comment": entry.Comment = value; break;
                    case "TryExec": entry.TryExec = value; break;
                    case "Hidden": entry.Hidden = value.Equals("true", StringComparison.OrdinalIgnoreCase); break;
                    case "NoDisplay": entry.NoDisplay = value.Equals("true", StringComparison.OrdinalIgnoreCase); break;
                }
            }
            if (string.IsNullOrEmpty(entry.Name) || string.IsNullOrEmpty(entry.Exec)) return null;
            return entry;
        }
        catch { return null; }
    }

    public string GetCleanExec()
    {
        // Remove field codes like %u, %f, %F, %U, etc.
        var parts = Exec.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts.Where(p => !p.StartsWith('%')));
    }
}
