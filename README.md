
# Dead Cells Core Modding (WIP)

![GitHub License](https://img.shields.io/github/license/dead-cells-core-modding/core) 
[![Build](https://github.com/dead-cells-core-modding/core/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/dead-cells-core-modding/core/actions/workflows/build.yml)


A Dead Cells Modding API/loader. 

> [!WARNING]
> This project is under active development. Breaking changes may be made to APIs with zero notice.

Download the latest build [here](https://nightly.link/dead-cells-core-modding/core/workflows/build/main)

## Roadmap

- [x] Simple Hook
- [x] Basic interoperability
- [ ] Game object proxy objects in .NET
- [x] Edit game bytecode dynamically
- [ ] More convenient BuildSystem
- [ ] Linux platform support

## Requirement

- .NET 9 runtime or higher
- Microsoft Visual C++ Redistributable package (2015-2022)

## Installation

1. Get the core file from [nightly link](https://nightly.link/dead-cells-core-modding/core/workflows/build/main) or [releases](https://github.com/dead-cells-core-modding/core/releases)
2. Unzip it to the game root directory

The folder structure should be similar to the following
```
<DeadCellsGameRoot>
|
+- coremod
|  |
|  +- core
|  |  |
|  |  +- native
|  |  |  |
|  |  |  +- ...
|  |  |
|  |  +- host
|  |  |  |
|  |  |  +- startup
|  |  |  |  |
|  |  |  |  +- DeadCellsModding.exe
|  |  |  |  |
|  |  |  |  +- ...
|  |  |  +- ...
|  |  +- ...
|  +- ...
|
+- deadcells.exe
|
+- deadcells_gl.exe
|
+- ...
```

## Usage

### Startup

Start the game from `<DeadCellsGameRoot>/coremod/core/host/startup/DeadCellsModding.exe`

## Development

### Requirement

- .NET 9 SDK
- NASM
- CMake

### Build

#### Windows

1. Clone the repository
2. Run `buildWin.bat`

## Credit

- [DeadCellsDecomp](https://github.com/N3rdL0rd/DeadCellsDecomp) and [alivecells](https://github.com/N3rdL0rd/alivecells) from N3rdL0rd
- [Hashlink](https://github.com/HaxeFoundation/hashlink) from HaxeFoundation

## License

Distributed under the MIT [license](https://github.com/DreamBoxSpy/DeadCellsCoreModding/blob/main/LICENSE).

## Disclaimer
Dead Cells Core Modding is in no way associated with Motion Twin.
