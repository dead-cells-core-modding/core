

echo "Building ModCore"

cd $PSScriptRoot/sources

dotnet build -c=Release ./ModCore

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

echo "Copying Goldberg"
$goldberg = $thirdparty + "/Goldberg/*"
Copy-Item -Path $goldberg -Destination "./goldberg" -Recurse -Force
