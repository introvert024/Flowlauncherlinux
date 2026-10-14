using System;
using System.Collections.Generic;
using System.IO;

namespace FlowLauncher.Linux.Infrastructure.XdgApplications;

public class XdgApplicationsScanner
{
    public static List<XdgAppEntry> ScanAll()
    {
        var results = new List<XdgAppEntry>();
        var dirs = new List<string>
        {
            "/usr/share/applications",
            "/usr/local/share/applications",
            "/var/lib/flatpak/exports/share/applications",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "applications"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "flatpak", "exports", "share", "applications")
        };

        // Also check XDG_DATA_DIRS
        var xdgDataDirs = Environment.GetEnvironmentVariable("XDG_DATA_DIRS") ?? string.Empty;
        foreach (var dir in xdgDataDirs.Split(':', StringSplitOptions.RemoveEmptyEntries))
            dirs.Add(Path.Combine(dir, "applications"));

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir)) continue;
            foreach (var file in Directory.EnumerateFiles(dir, "*.desktop", SearchOption.AllDirectories))
            {
                if (!seen.Add(Path.GetFileName(file))) continue;
                var entry = XdgAppEntry.Parse(file);
                if (entry != null && !entry.Hidden && !entry.NoDisplay)
                    results.Add(entry);
            }
        }
        return results;
    }
}
