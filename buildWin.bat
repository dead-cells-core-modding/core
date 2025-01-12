@echo off

echo Building ModCore
cd /d %~dp0/sources
dotnet build -c=Release ./ModCore

echo Building Shell
dotnet publish -c=Release -a x86 ./DeadCellsModding

echo Building Native
cd native
cmake . --preset=win-x86-release
cmake --build ./out/build/win-x86-release