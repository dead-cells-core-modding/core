param (
    [bool]$BuildNative = $true,
    [bool]$BuildMDK = $true
)
cd $PSScriptRoot

echo "Generating Haxe Proxy"
mkdir "./bin/core/mdk/ref" -Force
dotnet run -c Release --no-launch-profile --project ./tools/HaxeProxyGenerator ./hlboots/hlboot-opengl-steam.dat ./bin/core/mdk/ref/GameProxy.dll

if($BuildMDK) {
    echo "Building MDK"
    dotnet build -c Release ./mdk
    mkdir "./bin/core/mdk" -Force
    Get-ChildItem -Path "./mdk/bin" | Copy-Item -Destination "./bin/core/mdk" -Force -Recurse
}

echo "Building ModCore"

cd sources

dotnet build -c Release ./ModCore
dotnet build -c Release ./ModCore.ModLoader.Default

echo "Building Shell"
dotnet publish -c Release -r win-x64 ./DeadCellsModding

if($BuildNative) {
    echo "Building Native"
    cd native
    cmake . --preset=win-x64-release
    cmake --build ./out/build/win-x64-release

    echo "Copying 3rd library"

    $nativedir = $PSScriptRoot + "/bin/core/native/win-x64"
    $thirdparty = $PSScriptRoot + "/3rd"

    cd $nativedir

    echo "Copying Goldberg"
    $goldberg = $thirdparty + "/Goldberg/win-x64/*"
    Copy-Item -Path $goldberg -Destination "./goldberg" -Recurse -Force

}


cd $PSScriptRoot
