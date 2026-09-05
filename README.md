
# Dead Cells Core Modding

![GitHub Release](https://img.shields.io/github/v/release/dead-cells-core-modding/core)
![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/dead-cells-core-modding/core/total)


![GitHub License](https://img.shields.io/github/license/dead-cells-core-modding/core) 
[![Build And Test](https://github.com/dead-cells-core-modding/core/actions/workflows/build.yml/badge.svg?branch=dev)](https://github.com/dead-cells-core-modding/core/actions/workflows/build.yml)
[![Discord](https://img.shields.io/discord/1526779758312947792?label=Discord)](https://discord.gg/je38EURPqN)



A Dead Cells Modding API/loader. 

See [Documentation](https://dead-cells-core-modding.github.io/docs/docs)

Download the latest build [here](https://nightly.link/dead-cells-core-modding/core/workflows/build/dev)

Join the [Discord server](https://discord.gg/7vp38qsYc4) for more help.

## Requirement

- [.NET 10 Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Microsoft Visual C++ Redistributable (2015-2022 x64)](https://aka.ms/vs/17/release/vc_redist.x64.exe)

## Installation

### Manual Installation
1. Get the core file from [nightly link](https://nightly.link/dead-cells-core-modding/core/workflows/build/dev) or [releases](https://github.com/dead-cells-core-modding/core/releases)
2. Unzip it to the game root directory

> ⚠️ **Important:** The folder must be named strictly **`coremod`** (singular, lowercase). Naming it `coremods` will cause `DeadCellsModding.exe` to fail resolving `DCCMShell.dll` and exit immediately without an error message.

The folder structure should be similar to the following:
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

### Steam Workshop Setup

When subscribing via Steam Workshop ([DCCM Core Modding API - Workshop ID: 3633185550](https://steamcommunity.com/sharedfiles/filedetails/?id=3633185550)):

1. Steam downloads the files to `<Steam>/steamapps/workshop/content/588650/3633185550/`.
2. Copy the contents of `win-x64/content/` (`core`, `plugins`, and `ModCoreVersion.txt`) directly into `<DeadCellsGameRoot>/coremod/`.
3. Launch the game using `<DeadCellsGameRoot>/coremod/core/host/startup/DeadCellsModding.exe`.  
   *(Optional: You can also replace the vanilla `deadcells.exe` in the root folder with the launcher stub in `core/host/startup/steam/deadcells.exe` for seamless Steam Library launching).*

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

The native build requires **OpenAL** and **SDL3** headers and libraries:

```pwsh
cd sources\native\3rd\hashlink\include

# OpenAL
Invoke-WebRequest -Uri "https://github.com/kcat/openal-soft/releases/download/1.23.1/openal-soft-1.23.1-bin.zip" -OutFile "OpenAL.zip"
Expand-Archive -LiteralPath "OpenAL.zip" -DestinationPath . -Force
Move-Item -Path "openal-soft-1.23.1-bin" -Destination "openal" -Force

# SDL3
Invoke-WebRequest -Uri https://github.com/libsdl-org/SDL/releases/download/release-3.4.10/SDL3-devel-3.4.10-VC.zip -OutFile ./SDL.zip
Expand-Archive -LiteralPath ./SDL.zip -DestinationPath . -Force
Move-Item -Path ./SDL3-3.4.10 -Destination ./sdl -Force
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

## Third-Party Open Source Libraries

This project uses the following open source libraries directly (bundled or linked):

| Library | License | Path |
| --- | --- | --- |
| [HashLink](https://github.com/HaxeFoundation/hashlink) | MIT | `sources/native/3rd/hashlink/` |
| [TinyCC](https://repo.or.cz/tinycc.git) | LGPL 2.1 | `sources/native/3rd/tinycc/` |
| [mbedTLS](https://github.com/Mbed-TLS/mbedtls) | Apache 2.0 / GPL 2.0+ | `sources/native/3rd/hashlink/include/mbedtls/` |
| [zlib](https://zlib.net/) | zlib | `sources/native/3rd/hashlink/include/zlib/` |
| [libpng](http://www.libpng.org/pub/png/libpng.html) | libpng | `sources/native/3rd/hashlink/include/png/` |
| [libjpeg-turbo](https://libjpeg-turbo.org/) | IJG / Modified BSD | `sources/native/3rd/hashlink/include/turbojpeg/` |
| [minimp3](https://github.com/lieff/minimp3) | CC0 1.0 | `sources/native/3rd/hashlink/include/minimp3/` |
| [libogg + libvorbis](https://xiph.org/vorbis/) | BSD-style (Xiph.org) | `sources/native/3rd/hashlink/include/vorbis/` |
| [SDL](https://www.libsdl.org/) | zlib | `sources/native/3rd/hashlink/include/sdl/` |
| [OpenAL Soft](https://openal-soft.org/) | LGPL 2.0 | `sources/native/3rd/hashlink/include/openal/` |
| [libuv](https://libuv.org/) | MIT | `sources/native/3rd/hashlink/include/libuv/` |
| [PCRE2](https://www.pcre.org/) | BSD | `sources/native/3rd/hashlink/include/pcre/` |
| [meshoptimizer](https://github.com/zeux/meshoptimizer) | MIT | `sources/native/3rd/hashlink/include/meshoptimizer/` |
| [MikkTSpace](https://github.com/mmikk/MikkTSpace) | zlib-like | `sources/native/3rd/hashlink/include/mikktspace/` |
| [V-HACD](https://github.com/kmammou/v-hacd) | BSD 3-Clause | `sources/native/3rd/hashlink/include/vhacd/` |
| [SQLite](https://www.sqlite.org/) | Public Domain | `sources/native/3rd/hashlink/include/sqlite/` |
| [hlsteam](https://github.com/dead-cells-core-modding/hdlls) | MIT | `sources/native/hdlls/libs/hlsteam/` |
| [hlgog](https://github.com/dead-cells-core-modding/hdlls) | MIT | `sources/native/hdlls/libs/hlgog/` |
| [Goldberg Emulator](https://gitlab.com/Mr_Goldberg/goldberg_emulator) | LGPL 3.0 | `3rd/Goldberg/` |
| [Steamworks.NET](https://github.com/rlabrecque/Steamworks.NET) | MIT | `3rd/Steamworks.NET/` |
| [RectpackSharp](https://github.com/ThomasMiz/RectpackSharp) | MIT | `3rd/RectpackSharp/` |
| [crashlink](https://github.com/dead-cells-core-modding/crashlink) | MIT | `3rd/crashlink/` |

### NuGet Packages

| Package | License |
| --- | --- |
| [Newtonsoft.Json](https://www.newtonsoft.com/json) | MIT |
| [Mono.Cecil](https://github.com/jbevain/cecil) | MIT |
| [MonoMod](https://github.com/MonoMod/MonoMod) | MIT |
| [Iced](https://github.com/icedland/iced) | MIT |
| [Fody](https://github.com/Fody/Fody) | MIT |
| [Serilog](https://serilog.net/) | Apache 2.0 |
| [StbImageSharp](https://github.com/StbSharp/StbImageSharp) | MIT |
| [StbImageWriteSharp](https://github.com/StbSharp/StbImageWriteSharp) | MIT |
| [K4os.Hash.xxHash](https://github.com/K4os/K4os.Hash.xxHash) | MIT |
| [SharpPdb](https://github.com/aziz-sharp/SharpPdb) | MIT |
| [System.IO.Hashing](https://github.com/dotnet/runtime) | MIT |
| [CsWin32](https://github.com/microsoft/CsWin32) | MIT |

## Code Signing Policy

<table>
  <tr>
    <td>
      <img alt="SignPath" src="https://signpath.org/assets/favicon-50x50.png" />
    </td>
    <td>
    Free code signing on Windows provided by <a href="https://signpath.io">SignPath.io</a>, certificate by <a href="https://signpath.org/">SignPath Foundation</a><br/>
    </td>
  </tr>
</table>

### Team Roles

- **Authors** (committers): [Members team](https://github.com/orgs/dead-cells-core-modding/teams/members)
- **Reviewers**: [Members team](https://github.com/orgs/dead-cells-core-modding/teams/members) — each change proposed by non-members must be reviewed by a team member
- **Approvers**: [Owners](https://github.com/orgs/dead-cells-core-modding/people?query=role%3Aowner) — each signing request must be approved by a team member trusted to decide if a release can be code signed

### Privacy Policy

This program will not transfer any information to other networked systems unless specifically requested by the user or the person installing or operating it.

### Artifact Configuration

All signed binaries have product name set to 'Dead Cells Core Modding' and product version set to the release version.

## License

Distributed under the MIT [license](https://github.com/DreamBoxSpy/DeadCellsCoreModding/blob/main/LICENSE).

## Disclaimer
Dead Cells Core Modding is in no way associated with Motion Twin.
