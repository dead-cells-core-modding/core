#!/bin/bash
set -e

# Resolve script location
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Main paths
MDK_ROOT="$SCRIPT_DIR"
PACKAGES_DIR="$MDK_ROOT/packages"
BIN_ROOT="$MDK_ROOT/tools"
GAME_PATH="$(realpath "$MDK_ROOT/../..")"

echo
echo "DCCM MDK Installer"
echo

echo "Detected paths"
echo "  MDK Root : $MDK_ROOT"
echo "  Game Root: $GAME_PATH"
echo "  Tools    : $BIN_ROOT"
echo "  Packages : $PACKAGES_DIR"
echo

# Validate MDK structure
echo "Validating MDK structure..."

REQUIRED_PATHS=(
    "$MDK_ROOT/build/build.props"
    "$MDK_ROOT/build/build.targets"
    "$BIN_ROOT"
    "$PACKAGES_DIR"
)

for path in "${REQUIRED_PATHS[@]}"; do
    if [ ! -e "$path" ]; then
        echo "[ERROR] Missing: $path"
        exit 1
    fi
done

echo "[OK] MDK structure looks valid"
echo

# Export variables for current shell session
export DCCM_MDK_ROOT="$MDK_ROOT"
export DEAD_CELLS_GAME_PATH="$GAME_PATH"
export DCCM_MDK_BIN_ROOT="$BIN_ROOT"
export PATH="$BIN_ROOT:$PATH"

echo "Environment variables exported"
echo

# Detect shell
SHELL_NAME="$(basename "$SHELL")"

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
        echo "Unknown shell '$SHELL_NAME', falling back to bash config"
        CONFIG_FILE="$HOME/.bashrc"
        ;;
esac

echo "Detected shell : $SHELL_NAME"
echo "Config file    : $CONFIG_FILE"
echo

mkdir -p "$(dirname "$CONFIG_FILE")"
touch "$CONFIG_FILE"

echo "Cleaning previous DCCM configuration..."
sed -i '/# DCCM MDK BEGIN/,/# DCCM MDK END/d' "$CONFIG_FILE"

echo "Writing shell configuration..."

if [ "$SHELL_NAME" = "fish" ]; then

cat >> "$CONFIG_FILE" <<EOF

# DCCM MDK BEGIN
set -gx DCCM_MDK_ROOT "$MDK_ROOT"
set -gx DEAD_CELLS_GAME_PATH "$GAME_PATH"
set -gx DCCM_MDK_BIN_ROOT "$BIN_ROOT"

if not contains "\$DCCM_MDK_BIN_ROOT" \$PATH
    set -gx PATH "\$DCCM_MDK_BIN_ROOT" \$PATH
end
# DCCM MDK END

EOF

else

cat >> "$CONFIG_FILE" <<EOF

# DCCM MDK BEGIN
export DCCM_MDK_ROOT="$MDK_ROOT"
export DEAD_CELLS_GAME_PATH="$GAME_PATH"
export DCCM_MDK_BIN_ROOT="$BIN_ROOT"

case ":\$PATH:" in
    *":\$DCCM_MDK_BIN_ROOT:"*) ;;
    *) export PATH="\$DCCM_MDK_BIN_ROOT:\$PATH" ;;
esac
# DCCM MDK END

EOF

fi

echo "[OK] Shell configuration updated"
echo

# Register NuGet source
echo "Configuring NuGet source..."

dotnet nuget remove source DeadCoreModdingMDK >/dev/null 2>&1 || true

dotnet nuget add source \
    "$PACKAGES_DIR" \
    --name DeadCoreModdingMDK \
    >/dev/null

echo "[OK] NuGet source registered"
echo

# Check package cache
PACKAGE_PROPS="$HOME/.nuget/packages/deadcellscoremodding.mdk/1.0.1/build/DeadCellsCoreModding.MDK.props"

if [ -f "$PACKAGE_PROPS" ]; then
    echo "Found installed MDK package"

    if grep -q '\\\\build\\\\' "$PACKAGE_PROPS"; then
        echo "Patching Windows path separators..."
        sed -i 's|\\build\\|/build/|g' "$PACKAGE_PROPS"
        echo "[OK] Package patched"
    fi
fi

echo
echo "Installation summary"
echo

echo "DCCM_MDK_ROOT        = $DCCM_MDK_ROOT"
echo "DEAD_CELLS_GAME_PATH = $DEAD_CELLS_GAME_PATH"
echo "DCCM_MDK_BIN_ROOT    = $DCCM_MDK_BIN_ROOT"
echo

echo "Validation"

[ -f "$DCCM_MDK_ROOT/build/build.props" ] \
    && echo "[OK] build.props found" \
    || echo "[ERROR] build.props missing"

[ -d "$DCCM_MDK_BIN_ROOT" ] \
    && echo "[OK] Tools directory found" \
    || echo "[ERROR] Tools directory missing"

[ -d "$PACKAGES_DIR" ] \
    && echo "[OK] Packages directory found" \
    || echo "[ERROR] Packages directory missing"

FOUND_OVERRIDES=0

for file in \
    "$HOME/Projects"/*/Directory.Build.props \
    "$PWD/Directory.Build.props"
do
    [ -f "$file" ] || continue

    if grep -q "DCCM_MDK_ROOT" "$file"; then
        echo
        echo "Warning:"
        echo "  $file overrides DCCM_MDK_ROOT"
        echo
        grep -n "DCCM_MDK_ROOT" "$file" || true
        FOUND_OVERRIDES=1
    fi
done

if [ "$FOUND_OVERRIDES" -eq 0 ]; then
    echo "[OK] No obvious DCCM_MDK_ROOT overrides detected"
fi

echo
echo "Done!"
