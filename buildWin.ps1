

echo "Building ModCore"

cd $PSScriptRoot/sources

dotnet build -c=Release ./ModCore

echo "Building Shell"
dotnet publish -c=Release -r win-x86 ./DeadCellsModding

echo "Building Native"
cd native
cmake . --preset=win-x86-release
cmake --build ./out/build/win-x86-release
