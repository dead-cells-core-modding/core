
cd $PSScriptRoot

echo "Building MDK"
dotnet build -c=Release ./mdk
Get-ChildItem -Path "./mdk/bin" | Copy-Item -Destination "./bin/core/mdk" -Force -Recurse

echo "Building ModCore"

cd sources

dotnet build -c=Release ./ModCore
dotnet build -c=Release ./ModCore.BuildSystem
dotnet build -c=Release ./ModCore.ModLoader.Default

echo "Building Shell"
dotnet publish -c=Release -r win-x64 ./DeadCellsModding

echo "Building Native"
cd native
cmake . --preset=win-x64-release
cmake --build ./out/build/win-x64-release


echo "Copying 3rd library"

$nativedir = $PSScriptRoot + "/bin/core/native/win-x64"
$thirdparty = $PSScriptRoot + "/3rd"
echo $PSScriptRoot
echo $nativedir
cd $nativedir

echo "Copying hlsteam"
$hlsteam = $thirdparty + "/hlsteam/*"
Copy-Item -Path $hlsteam -Destination "./hlsteam" -Recurse -Force

#echo "Copying Goldberg"
#$goldberg = $thirdparty + "/Goldberg/*"
#Copy-Item -Path $goldberg -Destination "./goldberg" -Recurse -Force
