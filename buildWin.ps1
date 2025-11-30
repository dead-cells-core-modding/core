param (
    [bool]$BuildNative = $true,
    [bool]$BuildMDK = $true
)

$ErrorActionPreference = "Stop"

cd $PSScriptRoot

echo "Generating Haxe Proxy"
mkdir "./bin/core/mdk/ref" -Force
dotnet run -c Release --no-launch-profile --project ./tools/HaxeProxyGenerator ./hlboots/hlboot-opengl-steam.dat ./bin/core/mdk/ref/GameProxy.dll

if($BuildMDK) {
    echo "Building MDK"
    dotnet publish /p:Platform="Any CPU" ./mdk
    mkdir "./bin/core/mdk" -Force
    Get-ChildItem -Path "./mdk/bin" | Copy-Item -Destination "./bin/core/mdk" -Force -Recurse

    $env:DCCM_MDK_ROOT = (Resolve-Path ./bin/core/mdk).Path
    echo "Set DCCM_MDK_ROOT to $env:DCCM_MDK_ROOT "

    dotnet nuget add source "./bin/core/mdk/packages"  --name DeadCoreModdingMDK

}

echo "Building ModCore"

cd sources

dotnet build -c Release ./DCCMShell
dotnet build -c Release ./ModCore.ModLoader.Default

dotnet build -c Release ./ModCore.Assets

echo "Building Shell"
dotnet publish -c Release -r win-x64 ./DeadCellsModding

if($BuildNative) {
    echo "Building Native"
    cd native
    cmake . --preset=win-x64-release
    cmake --build ./out/build/win-x64-release

    $nativedir = $PSScriptRoot + "/bin/core/native/win-x64"

    echo "Scan non public members"
    dotnet run -c Release --no-launch-profile --project $PSScriptRoot/tools/NonPublicMemberScanner $nativedir hljit.dll libhl.dll

    echo "Copying 3rd library"

    
    $thirdparty = $PSScriptRoot + "/3rd"

    cd $nativedir

    echo "Copying Goldberg"
    $goldberg = $thirdparty + "/Goldberg/win-x64/*"
    mkdir goldberg
    Copy-Item -Path $goldberg -Destination "./goldberg" -Recurse -Force

}


cd $PSScriptRoot
