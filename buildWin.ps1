param (
    [bool]$BuildNative = $true,
    [bool]$BuildMDK = $true,
    [bool]$BuildCore = $true,
    [bool]$BuildAssets = $true
    [bool]$Debug = $false
)

$ErrorActionPreference = "Stop"

cd $PSScriptRoot

$build_conf = "Release"

if($Debug) {
    $build_conf = "Debug"
}

if($BuildNative) {

    echo "Building Native"
    cd sources/native
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

    cd $PSScriptRoot
}



if($BuildMDK) {
    echo "Generating Haxe Proxy"
    mkdir "./bin/core/mdk/ref" -Force
    dotnet run -c Release --no-launch-profile --project ./tools/HaxeProxyGenerator ./hlboots/hlboot-opengl-steam.dat ./bin/core/mdk/ref/GameProxy.dll

    echo "Building MDK"
    dotnet publish /p:Platform="Any CPU" ./mdk
    mkdir "./bin/core/mdk" -Force
    Get-ChildItem -Path "./mdk/bin" | Copy-Item -Destination "./bin/core/mdk" -Force -Recurse

    cd $PSScriptRoot
}

if($BuildCore) {
    echo "Building ModCore"

    cd sources

    dotnet build -c $build_conf ./DCCMShell
    dotnet build -c $build_conf ./ModCore.ModLoader.Default

    echo "Building Shell"
    dotnet publish -c Release -r win-x64 ./DeadCellsModding
    dotnet publish -c $build_conf -r win-x64 ./SteamStartShell

    cd $PSScriptRoot
}

if($BuildAssets) {
     cd sources

     dotnet build -c $build_conf ./ModCore.Assets
     cd $PSScriptRoot
}


cd $PSScriptRoot
