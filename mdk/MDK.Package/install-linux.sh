#!/bin/bash
set -e

# Determine paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MDK_ROOT="$(realpath "$SCRIPT_DIR/../../")"

PACKAGES_DIR="$MDK_ROOT/core/mdk/packages"

echo "DCCM MDK Linux Installer"
echo "MDK Root: $MDK_ROOT"
echo "Packages directory: $PACKAGES_DIR"

# Export variables for current session
export DCCM_MDK_ROOT="$MDK_ROOT"
export DEAD_CELLS_GAME_PATH="$(realpath "$MDK_ROOT/../..")"
export DCCM_MDK_BIN_ROOT="$MDK_ROOT/tools"

echo "Environment variables set for current session."

# Detect shell and add to config
SHELL_NAME=$(basename "$SHELL")

case "$SHELL_NAME" in
    bash) CONFIG_FILE="$HOME/.bashrc" ;;
    zsh)  CONFIG_FILE="$HOME/.zshrc" ;;
    fish) CONFIG_FILE="$HOME/.config/fish/config.fish" ;;
    *)    CONFIG_FILE="$HOME/.bashrc" ;;
esac

echo "Detected shell: $SHELL_NAME (config: $CONFIG_FILE)"

# Add variables to shell config if not already present
if [ "$SHELL_NAME" = "fish" ]; then
    if ! grep -q "DCCM_MDK_ROOT" "$CONFIG_FILE" 2>/dev/null; then
        cat >> "$CONFIG_FILE" << EOF

# DCCM MDK
set -x DCCM_MDK_ROOT "$MDK_ROOT"
set -x DEAD_CELLS_GAME_PATH "$DEAD_CELLS_GAME_PATH"
set -x DCCM_MDK_BIN_ROOT "$DCCM_MDK_BIN_ROOT"
set -x PATH "\$DCCM_MDK_BIN_ROOT" \$PATH
EOF
    fi
else
    if ! grep -q "DCCM_MDK_ROOT" "$CONFIG_FILE" 2>/dev/null; then
        cat >> "$CONFIG_FILE" << EOF

# DCCM MDK
export DCCM_MDK_ROOT="$MDK_ROOT"
export DEAD_CELLS_GAME_PATH="$DEAD_CELLS_GAME_PATH"
export DCCM_MDK_BIN_ROOT="$DCCM_MDK_BIN_ROOT"
export PATH="\$DCCM_MDK_BIN_ROOT:\$PATH"
EOF
    fi
fi

# Register NuGet source
echo "Registering NuGet source..."
dotnet nuget remove source DeadCoreModdingMDK 2>/dev/null || true
dotnet nuget add source "$PACKAGES_DIR" --name DeadCoreModdingMDK

echo ""
echo "Done!"
echo ""
