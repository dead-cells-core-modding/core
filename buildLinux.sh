#!/bin/bash

# 参数处理
BuildNative=true
BuildMDK=true

while [[ $# -gt 0 ]]; do
    case "$1" in
        --no-native)
            BuildNative=false
            shift
            ;;
        --no-mdk)
            BuildMDK=false
            shift
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

SCRIPT_DIR=$(dirname "$(readlink -f "$0")")
cd "$SCRIPT_DIR"

echo "Generating Haxe Proxy"
mkdir -p "./bin/core/mdk/ref"
dotnet run -c Release --no-launch-profile --project ./tools/HaxeProxyGenerator ./hlboots/hlboot-directx-steam.dat ./bin/core/mdk/ref/GameProxy.dll

if $BuildMDK; then
    echo "Building MDK"
    dotnet build -c Release ./mdk
    mkdir -p "./bin/core/mdk"
    cp -rf ./mdk/bin/* ./bin/core/mdk/
fi

echo "Building ModCore"

cd sources

dotnet build -c Release ./ModCore
dotnet build -c Release ./ModCore.ModLoader.Default

echo "Building Shell"
dotnet publish -c Release -r linux-x64 ./DeadCellsModding

if $BuildNative; then
    echo "Building Native"
    cd native
    cmake . --preset=linux-release
    cmake --build ./out/build/linux-release

    echo "Copying 3rd library"

    nativedir="$SCRIPT_DIR/bin/core/native/linux-x64"
    thirdparty="$SCRIPT_DIR/3rd"

    cd "$nativedir"

    echo "Copying Goldberg"
    mkdir -p "./goldberg"
    cp -rf "$thirdparty/Goldberg/linux-x64/"* "./goldberg/"
fi

cd "$SCRIPT_DIR"