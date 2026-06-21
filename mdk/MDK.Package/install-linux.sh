#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MDK_ROOT="$(realpath "$SCRIPT_DIR/../../")"


export DCCM_MDK_ROOT="$MDK_ROOT"
export DEAD_CELLS_GAME_PATH="$(realpath "$MDK_ROOT/../..")"
export DCCM_MDK_BIN_ROOT="$MDK_ROOT/tools"

SHELL_NAME=$(basename "$SHELL")

case "$SHELL_NAME" in
    bash)
        CONFIG_FILE="$HOME/.bashrc"
        ;;
    zsh)
        CONFIG_FILE="$HOME/.zshrc"
        ;;
    fish)
        CONFIG_FILE="$HOME/.config/fish/config.fish"
        ;;
    *)
        CONFIG_FILE="$HOME/.bashrc"
        ;;
esac

if [ "$SHELL_NAME" = "fish" ]; then
    if ! grep -q "DCCM_MDK_ROOT" "$CONFIG_FILE" 2>/dev/null; then
        cat >> "$CONFIG_FILE" << EOF

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

dotnet nuget remove source DeadCoreModdingMDK 2>/dev/null || true
dotnet nuget add source "$MDK_ROOT/packages" --name DeadCoreModdingMDK
