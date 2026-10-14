#!/bin/bash
rm -rf "$HOME/.local/lib/flow-launcher"
rm -f "$HOME/.local/bin/flow-launcher"
rm -f "$HOME/.local/share/applications/flow-launcher.desktop"
rm -f "$HOME/.config/autostart/flow-launcher.desktop"
update-desktop-database "$HOME/.local/share/applications" 2>/dev/null || true
echo "Flow Launcher uninstalled."
