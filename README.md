
# Dead Cells Core Modding

![GitHub License](https://img.shields.io/github/license/dead-cells-core-modding/core) 
[![Build And Test](https://github.com/dead-cells-core-modding/core/actions/workflows/build.yml/badge.svg?branch=dev)](https://github.com/dead-cells-core-modding/core/actions/workflows/build.yml)


A Dead Cells Modding API/loader. 

[Documentation](https://dead-cells-core-modding.github.io/docs/docs)

Download the latest build [here](https://nightly.link/dead-cells-core-modding/core/workflows/build/dev)

## Roadmap

- [ ] More convenient BuildSystem
- [ ] Linux platform support

## Requirement

- .NET 10 runtime
- Microsoft Visual C++ Redistributable package (2015-2022)

## Installation

1. Get the core file from [nightly link](https://nightly.link/dead-cells-core-modding/core/workflows/build/dev) or [releases](https://github.com/dead-cells-core-modding/core/releases)
2. Unzip it to the game root directory

The folder structure should be similar to the following
```txt
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
|  |  +- mdk
|  |  |	 |
|  |  |  +- install.ps1
|  |  |  |
|  |  |  +- uninstall.ps1
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

## Mods Development

Here are some [examples](https://github.com/dead-cells-core-modding/core/tree/main/sample).

### Preparation

1. Install .NET SDK 10
2. Install Dead Cells Core Modding as above
3. Run `<DeadCellsGameRoot>/coremod/core/mdk/install.ps1` to configure the environment

### Create a mod project

1. Create a library project based on .NET 10
2. Add package reference `DeadCellsCoreModding.MDK`
3. Add the following to your csproj file
```xml
<PropertyGroup>
	<!--Enter the mod name here-->
	<ModName>$(AssemblyName)</ModName>

	<!--
	Enter mod type here

	Available values:
		mod: Normal mod
		library: Library
	-->
	<ModType>mod</ModType>

	<!--Enter the full name of the mod's main type here-->
	<ModMain>ModNamespace.MainModClass</ModMain>
</PropertyGroup>
```

### Build

Build the mod using `dotnet build`.
The default output directory is `$(OutputPath)/output/`

## Usage

### Startup

Start the game from `<DeadCellsGameRoot>/coremod/core/host/startup/DeadCellsModding.exe`

### Mods Installation

1. Create `<DeadCellsGameRoot>/coremod/mods` folder if it does not exist.
2. Move the mods files into the `mods` folder. The folder structure should look like this:
```txt
mods
|
+- <ModName>
|  |
|  +- modinfo.json
|  |
|  +- ...
|
+- ...
```

> [!WARNING]
> `<ModName>` must be exactly the same as the `name` property in `modinfo.json`, otherwise the mods loader will refuse to load the mods

## Development

### Requirement

- .NET SDK 10
- CMake
- MSVC

### Download Additional Dependencies

The native build requires **OpenAL** and **SDL2** headers and libraries:

```pwsh
cd sources\native\3rd\hashlink\include

# OpenAL
Invoke-WebRequest -Uri "https://github.com/kcat/openal-soft/releases/download/1.23.1/openal-soft-1.23.1-bin.zip" -OutFile "OpenAL.zip"
Expand-Archive -LiteralPath "OpenAL.zip" -DestinationPath . -Force
Move-Item -Path "openal-soft-1.23.1-bin" -Destination "openal" -Force

# SDL2
Invoke-WebRequest -Uri "https://www.libsdl.org/release/SDL2-devel-2.32.8-VC.zip" -OutFile "SDL.zip"
Expand-Archive -LiteralPath "SDL.zip" -DestinationPath . -Force
Move-Item -Path "SDL2-2.32.8" -Destination "sdl" -Force
```

### Build

```pwsh
# Full build (Debug)
.\build.ps1

# Full build (Release)
.\build.ps1 --configuration Release

# Individual targets
.\build.ps1 BuildNative                  # Native runtime only
.\build.ps1 BuildCore                    # Managed core only
.\build.ps1 BuildMDK                     # MDK toolchain only
.\build.ps1 BuildAssets                  # Assets project only

# Combine multiple targets
.\build.ps1 BuildCore BuildMDK BuildAssets --configuration Release

```

> ⚠️ Before running BuildAssets, the MDK must be installed. See the step below.

#### Install MDK

After building the MDK, install it by running the setup script:

```pwsh
.\build.ps1 BuildMDK
.\bin\core\mdk\install.ps1
```

### Output

Build artifacts are placed in the `bin/` directory:

```
bin/
├── core/
│   ├── host/startup/          
│   │   └── DeadCellsModding.exe # Game launcher
│   ├── mdk/                   # MDK toolchain
│   └── native/
│       └── win-x64/           # Native runtime
│           └── goldberg/      # Goldberg Steam emulator
└── ...
```


## Credit

- [MonoMod](https://github.com/MonoMod/MonoMod)
- [HashlinkNET](https://github.com/DreamBoxSpy/HashlinkNET) from DreamBoxSpy
- [sharplink](https://github.com/steviegt6/sharplink) from Tomat
- [DeadCellsDecomp](https://github.com/N3rdL0rd/DeadCellsDecomp) and [alivecells](https://github.com/N3rdL0rd/alivecells) from N3rdL0rd
- [Hashlink](https://github.com/HaxeFoundation/hashlink) from HaxeFoundation

## License

Distributed under the MIT [license](https://github.com/DreamBoxSpy/DeadCellsCoreModding/blob/main/LICENSE).

## Disclaimer
Dead Cells Core Modding is in no way associated with Motion Twin.
