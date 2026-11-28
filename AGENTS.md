# AGENTS.md — Dead Cells Core Modding

> Instructions for AI coding agents working on this project.

## Project Identity

**DeadCellsCoreModding** is a modding framework for *Dead Cells* (Motion Twin / Evil Empire). It intercepts the game's Hashlink VM (the Haxe-based runtime Dead Cells runs on) and injects managed .NET code, allowing mods written in C# to interact with and modify the game at runtime.

| Property | Value |
|---|---|
| **Version** | `35.16.1` (in `Directory.Build.props`) |
| **License** | MIT |
| **Target Framework** | .NET 11 (`net11.0`, `runtime-async=on`) |
| **Platforms** | Windows x64, Linux x64 (arm64 reserved for Android) |
| **Game Stores** | Steam, GOG |
| **Repo** | `github.com/dead-cells-core-modding/core` |
| **Docs** | https://dead-cells-core-modding.github.io/docs/docs |

---

## Architecture Overview

### Boot Flow

```
deadcells.exe / DeadCellsModding.exe (launcher shell)
  → SteamStartShell / GOGStartShell (Windows) or DeadCellsModding
    → nethost locates .NET runtime
    → loads DCCMShell.dll (sources/DCCMShell/Shell.cs)
      → Startup.StartGame() (sources/ModCore/Startup.cs)
        → Checks .NET 11+ runtime
        → Serilog initialization (LogInitializer)
        → Prints startup banner (version, repository, license)
        → Core.Initialize() (sources/ModCore/Core.cs)
          → Assembly resolve hooks
          → Loads [CoreModule(CoreModuleKind.Preload)] modules
          → Broadcasts IOnCoreModuleInitializing
        → Loads hlboot.dat bytecode (from hlboots/, or embedded in deadcells_gl.exe)
        → Native.InitializeGame() → starts Hashlink VM
        → Broadcasts lifecycle events (IOnGameInit, IOnFrameUpdate, etc.)
```

On Steam, `SteamStartShell` (published as `deadcells.exe`) wraps `SteamLauncher`, which performs
Workshop sync/self-update and then launches `DeadCellsModding`.

### Two-Layer Module System

1. **Core Modules** (`CoreModule<T>`) — Built-in framework services. Annotated with `[CoreModule(CoreModuleKind.Preload | Normal)]`. Filtered by OS support. Singletons via `Module<T>.Instance`. Ordered by priority constants in `ModulePriorities.cs`.

2. **User Mods** (`ModBase`) — Third-party mods loaded from `coremod/mods/` by `ModLoader`. Discovered via `modinfo.json` metadata. Implement lifecycle event interfaces.

Plugins are loaded separately via `PluginLoader` — classes with `[Plugin]` attribute in discovered DLLs.

### Event System (Interface-Based Pub/Sub)

Modules implement event interfaces (e.g., `IOnGameInit`, `IOnFrameUpdate`, `IOnBeforeGameInit`). The `EventSystem` (in `ModCore.Common`) broadcasts by interface type and discovers all receivers. Registration is automatic in the `Module` constructor via `EventSystem.AddReceiver(this)`.

---

## Directory Map

### `sources/` — Main C# Solution (`DeadCellsCoreModding.slnx`)

| Project | Role |
|---|---|
| **ModCore** | Core modding framework — initialization, module system, event bus, mod loading, hooks, plugins, storage, menus, serialization |
| **ModCore.Common** | Shared utilities — `EventSystem`, collections, storage abstractions, native helpers (namespace `ModCore`) |
| **ModCore.Assets** | Asset bundling (requires MDK installed) |
| **ModCore.Native** | Native interop — P/Invoke, TCC JIT compiler, assembly code gen, memory mgmt, platform abstraction |
| **ModCore.Native.Fody** | Fody IL weaver — compile-time native call stub generation |
| **ModCore.ModLoader.Default** | Default mod/plugin loader — reads `modinfo.json`, constructs `ModBase` instances |
| **DCCMShell** | Bridge DLL called by the launcher; sets up env and invokes `Startup.StartGame()` |
| **DeadCellsModding** | NativeAOT launcher EXE — uses `nethost` to load .NET runtime and DCCMShell |
| **SteamLauncher** | Steam Workshop sync, self-update, and game launch logic used by `SteamStartShell` |
| **SteamStartShell** | Steam-specific launcher (published as `deadcells.exe`; workshop support, error reporting) |
| **GOGStartShell** | GOG-specific launcher |
| **HashlinkSharp** | Hashlink VM bridge — wraps native HL API in C# (threads, marshaling, reflection) |
| **HashlinkNET.Bytecode** | HL bytecode reader — parses `.hlb` into C# object model |
| **HashlinkNET.Compiler** | HL bytecode → .NET assembly compiler (pseudocode IR emission) |
| **HaxeProxy** | Runtime proxy generation — C# proxies for Haxe types |
| **HaxeDocs** | Haxe documentation parser |
| **NonPublicNativeMembers** | Native member scanner — reads PDB/ELF for non-public function signatures |

### `sources/ModCore/` Internal Structure

| Directory | Purpose |
|---|---|
| `Events/Interfaces/` | Lifecycle event interfaces (`IOnGameInit`, `IOnFrameUpdate`, `IOnBeforeGameInit`, `IOnSaveConfig`, etc.) |
| `Events/Interfaces/Game/` | Game-specific events (CDB, Pak, Save, Hero, Menu) |
| `Events/Interfaces/Mods/` | Mod discovery/registration events |
| `Events/Interfaces/VM/` | Hashlink VM and native resolution events |
| `Modules/` | Core modules (`Game`, `GameStartup`, `ModLoader`, `HashlinkHooks`, `HashlinkVM`, `CDBManager`, `SaveManager`, `StorageManager`, `MenuModule`, `FsPak`, `GetText`, `NativeHooks`) |
| `Modules/Internals/` | Internal modules (`PluginLoader`, `NativeModuleResolver`, `HaxeProxyGenerator`, `HxbitModule`, `HarmonyXModule`, `SFXModule`, `LegacySFX`, `AtlasRefresher`) |
| `Hooks/` | `HashlinkHookManager`, `HashlinkFunctionPatcher`, `HookOrigClosure` — dynamic IL adapters for game hooks |
| `Menu/` | `IModMenu` / `IModMenuProvider` interfaces |
| `Mods/` | `ModBase` (user mod base class), `ModInfo` (JSON metadata) |
| `Plugins/` | `PluginBase` + `[Plugin]` attribute |
| `Storage/` | `Config<T>` (JSON config), `SaveData<T>` (per-save data), `IHxbitSerializable` |
| `Serialization/` | Hxbit serialization (game save format) |
| `Utilities/` | String/Array/Bytes/Color/Enum/Process utilities |

### `sources/native/` — Native Runtime (C/C++, CMake)

| Subdirectory | Role |
|---|---|
| `modcorenative/` | Core native library (`modcorenative.dll/.so`) — links Hashlink with .NET nethost and hosts the native bridge |
| `3rd/hashlink/` | Vendored Hashlink VM source (MIT, git submodule) |
| `3rd/tinycc/` | Vendored TinyCC JIT compiler (LGPL 2.1, git submodule) |
| `tinycc/` | Build shim for the bundled TinyCC (`tinycc.c`), used by `AsmAssembler` at runtime |
| `hdlls/` | Custom Hashlink DLLs — Steamworks/GOG plugins (git submodule) |
| `host_lib/` | Host library for NativeAOT linking |

### `mdk/` — Mod Development Kit (separate solution `mdk.slnx`)

The MDK is the toolchain mod authors install. It includes MSBuild targets (`ModCore.Tasks`,
`build.props`/`build.targets`), proxy assemblies, and the `DCCMTool` CLI.

| Project | Role |
|---|---|
| **DCCMTool** | CLI used by build + mods — game proxy generation, CDB/pak tools, native member scanning, Workshop upload |
| **ModCore.Tasks** | MSBuild task assembly (`ResolveModDependencies`, modinfo generation) |
| **DeadCellsCoreModding.MDK** | MSBuild `.props`/`.targets` imported by mod projects |
| **MDK.Package** | Packs/installs the MDK (`install.ps1`, `install-linux.sh`) |
| **GameRes.Core** | CDB/pak/atlas resource types shared with tools |

### `tools/` — Developer Utilities (separate solution `tools.slnx`)

`BytecodeMapping`, `HaxeProxyGenerator`, `NonPublicMemberScanner`.

### `sample/` — Example Mods (separate solution `sample.slnx`)

| Project | Type | What it demonstrates |
|---|---|---|
| `SampleSimple` | mod | Minimal mod: lifecycle events, asset loading |
| `SampleWeapon` | mod | Hashlink hooking, custom weapon, CDB diff, hxbit serialization |
| `SampleHook` | mod | Hook patterns |
| `DoubleCells` | mod | Gameplay modification |
| `DebugMod` | mod | Debug utilities |
| `LibraryMod` | library | Shared library pattern |

### `test/` — Integration Tests & Benchmarks

- **TestRunner** — xUnit v3 test project on Microsoft.Testing.Platform (single-threaded, 600s timeout)
- **TestMod** — Minimal mod used during integration tests
- **Benchmark** — BenchmarkDotNet project for marshaling, hooks, proxies, and event dispatch
- Test categories include `GcTest`, `HashlinkTest`, `HaxeProxyTest`, `ModLoaderTest`, `ObjectInheritanceTest`, `ExceptionTest`, `HookExceptionTest`, `GameProxyJitTest`

### `3rd/` — Vendored Third-Party Binaries

| Library | License | Purpose |
|---|---|---|
| Goldberg | LGPL 3.0 | Steam emulator for non-Steam environments |
| Steamworks.NET | MIT | Steamworks SDK .NET bindings |
| RectpackSharp | MIT | Rectangle packing |
| Mesa-d3d12 | MIT | D3D12 Mesa runtime for Windows |

### Other Directories

| Directory | Purpose |
|---|---|
| `hlboots/` | Precompiled game boot bytecode (`.dat` files for Steam/GOG) |
| `build/` | NUKE build project (C# build automation) |
| `bin/` | Build output — native DLLs, managed assemblies, MDK, logs, mods |
| `workshop-publish/` | Steam Workshop publishing staging |
| `.opencode/` | OpenCode commands and configuration (`commands/commit.md`, `commands/release-info.md`) |

---

## Build System

### NUKE (Primary Build Orchestrator)

Build logic is in `build/Build.cs` — a NUKE C# build project. Bootstrapped via `build.cmd`/`build.ps1`/`build.sh` which auto-download the .NET SDK if missing.

**Build Targets (in dependency order):**

| Target | What it does |
|---|---|
| `BuildNative` | CMake build of native runtime (modcorenative, hljit, libhl) + TinyCC headers + Goldberg copy + Mesa d3d12 copy + native member scanning via DCCMTool |
| `GenerateGameProxy` | Runs DCCMTool to generate per-boot proxies from `hlboots/*.dat` and merges `GameProxy.dll` |
| `BuildCore` | Builds DCCMShell + ModCore.ModLoader.Default; publishes SteamStartShell/GOGStartShell (Windows) and DeadCellsModding |
| `BuildMDK` | Builds `mdk/mdk.slnx` and publishes/copies to `bin/core/mdk/` |
| `BuildAssets` | Builds ModCore.Assets (requires MDK installed first) |
| `BuildAll` | Default target — BuildNative → BuildCore → BuildMDK → BuildAssets |
| `DownloadWin64Bin` / `DownloadLinux64Bin` | Fetch latest release archives into `workshop-publish/` |
| `GenerateReleaseInfo` | Uses OpenCode to generate release notes |
| `Release` | Commits release info, tags `v<version>`, and pushes |
| `PublishMAPI` | Uploads to Steam Workshop |
| `PublishDiscordAnnouncement` | Posts the release to Discord via webhook |

### Native Build (CMake + Ninja)

- **Presets**: `win-x64-{debug,release}`, `linux-x64-{debug,release}`, `android-x64-{debug,release}`, `android-arm64-{debug,release}` (arm64 reserved)
- **Compilers**: MSVC (Windows), GCC (Linux), NDK (Android)
- **C Standard**: C11, **C++ Standard**: C++17
- **Output**: `bin/core/native/{platform}-{arch}/`

### Key MSBuild Properties

From root `Directory.Build.props`:
```xml
<Version>35.16.1</Version>
<TargetFramework>net11.0</TargetFramework>
<AllowUnsafeBlocks>true</AllowUnsafeBlocks>
<Platforms>AnyCPU;x64</Platforms>
<EmbedUntrackedSources>true</EmbedUntrackedSources>
<CETCompat>false</CETCompat>
<Features>runtime-async=on</Features>
```

`Nullable` and `ImplicitUsings` are enabled per-project (test and sample projects set them
explicitly) rather than globally in `Directory.Build.props`.

### Build Output Structure

```
bin/core/
├── host/startup/       — DeadCellsModding.exe (launcher) + steam/gog shells (deadcells.exe)
├── host/               — ModCore.dll, DCCMShell.dll, all managed assemblies
├── mdk/                — MDK toolchain (install.ps1, ref/, bin/, tools/)
└── native/{platform}/  — modcorenative.dll, libhl.dll, hljit.dll, hdlls, goldberg/, mesa-d3d12/
```

---

## Coding Conventions

### `.editorconfig` Rules (`sources/.editorconfig`)

| Rule | Value |
|---|---|
| Indentation | 4 spaces, tabs = 4 |
| Line endings | LF |
| Encoding | UTF-8 |
| Trailing newline | Required |
| `var` usage | Preferred for built-in types, when type is apparent, and elsewhere |
| Namespaces | Block-scoped (`namespace X { ... }`) |
| `this.` qualification | Disabled |
| Expression-bodied | Properties/indexers/accessors = yes; methods/constructors = no |
| Pattern matching | Prefer `is` over `as`, switch expressions, `not` patterns |
| Primary constructors | Preferred (`class Foo(Bar bar) : Base(bar)`) |
| Nullable | Enabled project-wide |
| Naming | PascalCase types/methods/properties; `I` prefix for interfaces |
| Unsafe blocks | Allowed (required for native interop) |

### XML Documentation

- `GenerateDocumentationFile=true` in `ModCore.csproj`
- All public types and members require `<summary>` XML doc comments
- Use `<param>`, `<returns>`, `<exception>`, `<typeparam>`, `<see cref="..."/>`
- Use `<inheritdoc/>` for inherited docs
- `#pragma warning disable CS1591` allowed for internal-only files (e.g., `ModulePriorities.cs`)

### Namespace Convention

Namespace matches folder structure:
```
ModCore.Events.Interfaces.Game.Save  →  Events/Interfaces/Game/Save/
ModCore.Modules.Internals            →  Modules/Internals/
ModCore.Utilities                    →  Utilities/
```

### Primary Constructor Pattern

Strongly preferred. Module/mod constructors use the primary constructor syntax:
```csharp
public class SimpleMod(ModInfo info) : ModBase(info), IOnGameInit { ... }
```

### Logging (Serilog)

- Structured logging throughout — use `Logger.Information("message {var}", value)` not string interpolation
- Per-module logger via `Log.ForContext("SourceContext", typeof(TModule).Name)`
- Log levels: `Information`, `Warning`, `Error(ex, ...)`, `Fatal(ex, ...)`
- Output: `logs/log_latest.log` + timestamped archives (max 31 files)

### Error Handling

- **Module boundaries**: `catch (Exception ex) { Logger.Error(ex, "message"); }` — log and continue
- **Top-level fatal**: `Logger.Fatal(ex, "Fatal Error")` then `Environment.Exit(-1)`
- **Hashlink boundary**: Use `ExceptionDispatchInfo` for re-throwing across the native/managed boundary
- Never silently swallow exceptions without at least logging

---

## Module & Event System (API Patterns)

### Creating a Core Module

```csharp
[CoreModule(CoreModuleKind.Preload)] // or .Normal
internal class MyModule : CoreModule<MyModule>, IOnGameInit
{
    public override int Priority => -500; // lower = earlier (see ModulePriorities.cs)

    void IOnGameInit.OnGameInit()
    {
        Logger.Information("Game initialized!");
    }
}

// Accessed anywhere via: MyModule.Instance
```

### Key Lifecycle Interfaces

| Interface | When Called |
|---|---|
| `IOnCoreModuleInitializing` | After preload modules load, before normal modules |
| `IOnAdvancedModuleInitializing` | During advanced module initialization |
| `IOnPluginInitializing` | During plugin initialization |
| `IOnPluginInitialized` | After all plugins initialized |
| `IOnBeforeGameInit` | Before game Haxe entry point executes |
| `IOnGameInit` | When game window is created |
| `IOnGameEndInit` | When game fully initialized |
| `IOnFrameUpdate` | Every frame |
| `IOnGameExit` | When game exits |
| `IOnSaveConfig` | On config save |
| `IOnAfterLoadingAssets` | After game resources loaded |
| `IOnAfterLoadingCDB` / `IOnLoadedPak` / `IOnUpdatedPakRecord` / `IOnLoadingLanguage` | CDB and Pak loading |
| `IOnBeforeLoadingSave` / `IOnAfterLoadingSave` / `IOnBeforeSavingSave` / `IOnAfterSavingSave` | Save lifecycle |
| `IOnBeforeSavingModdedSave` / `IOnAfterLoadingModdedSave` | Modded save data |
| `IOnHashlinkVMReady` / `IOnResolveNativeLib` / `IOnResolveNativeFunction` | Hashlink VM and native resolution |
| `IOnHeroInit` / `IOnHeroUpdate` / `IOnHeroDispose` | Hero lifecycle |

Full list in `sources/ModCore/Events/Interfaces/`.

### Hashlink Hook Pattern

```csharp
// In IOnGameInit or IOnAfterLoadingAssets:
var hooks = HashlinkHooks.Instance;
hooks.CreateHook("tool.$Weapon", "create", Hook_WeaponCreate.Hook_create).Enable();

// Hook handler:
public delegate Weapon orig_create(Hero hero, InventItem item);
public static Weapon Hook_create(orig_create orig, Hero hero, InventItem item)
{
    // Call orig(...) to invoke the original game function
    // Return custom/modified value to override
}
```

### Config Pattern

```csharp
public class MyConfig
{
    public bool Enabled { get; set; } = true;
    public int Value { get; set; } = 42;
}

// Singleton, auto-loaded from JSON on startup, auto-saved on exit:
var config = new Config<MyConfig>("my_config_key");
config.Data.Enabled = false;
config.Save(); // Or wait for IOnSaveConfig
```

### Save Data Pattern

```csharp
var saveData = new SaveData<MyDataType>("unique_save_key");
// Automatically serialized/deserialized with game saves
// Implement IOnBeforeSavingModdedSave / IOnAfterLoadingModdedSave
```

---

## Testing

### Framework

- **xUnit v3** (`xunit.v3` NuGet, v4.0.2) on Microsoft.Testing.Platform
- **Test project**: `test/TestRunner/TestRunner.csproj`
- **Test mod**: `test/TestMod/` — compiled and loaded during integration tests
- **Custom framework**: `DCCMTestFramework` extending `XunitTestFramework` (handles rerun detection)
- **Benchmarks**: `test/Benchmark/` — BenchmarkDotNet project

### Configuration

- Single-threaded execution (`parallelizeAssembly: false`)
- Long test timeout: 600 seconds
- Test categories in separate files: `ExceptionTest.cs`, `GcTest.cs`, `HashlinkTest.cs`, `HaxeProxyTest.cs`, `ModLoaderTest.cs`, `ObjectInheritanceTest.cs`, `GameProxyJitTest.cs`, `HarmonyXTest.cs`

### Test Patterns

```csharp
public class MyTest
{
    [Fact]
    public void MyTestMethod()
    {
        HashlinkMarshal.EnsureThreadRegistered(); // Required for HL ops
        Assert.True(condition);
        Assert.Equal(expected, actual);
    }
}
```

### Running Tests

```bash
dotnet test test/TestRunner/TestRunner.csproj
```

---

## CI/CD (GitHub Actions)

**File**: `.github/workflows/build.yml`

### Jobs

1. **`build`** (matrix: win/linux × debug/release)
   - Setup .NET SDK (11) → download deps → build native + core + MDK + assets
   - Generate SPDX SBOM → attest artifacts

2. **`test`** (depends on build; Windows, debug, matrix: steam/gog)
   - Setup env → install MDK → `dotnet test`
   - Upload crash dumps on failure

3. **`benchmark`** (depends on build, windows)
   - Downloads build artifacts, copies `hlboot.dat`, runs BenchmarkDotNet

4. **`test-build-samples`** (depends on build, windows)
   - Install MDK → build all sample mods

5. **`sign-win`** (on tag push) — code-sign Windows artifacts via SignPath

6. **`publish-release`** (on tag push)
   - Pack artifacts → create GitHub Release with body from `latest-release.md`

---

## External Dependencies

### Git Submodules

| Path | Repository |
|---|---|
| `sources/native/3rd/hashlink` | `HaxeFoundation/hashlink` — HashLink VM |
| `sources/native/hdlls` | `dead-cells-core-modding/hdlls` — Custom HashLink DLLs |
| `sources/native/3rd/tinycc` | `dead-cells-core-modding/tinycc` — TinyCC fork |

### Key NuGet Packages

| Package | Used For |
|---|---|
| `Mono.Cecil` 0.11.6 | IL manipulation |
| `MonoMod.Core` 1.4.0-prerelease.3 | Core detour/IL helpers |
| `MonoMod.RuntimeDetour` 25.4.0-prerelease.3 | Runtime detours/hooks |
| `HarmonyX` 2.16.1 | Harmony patch compatibility |
| `Serilog` 4.4.0 | Structured logging |
| `Newtonsoft.Json` 13.0.4 | JSON serialization |
| `Microsoft.Windows.CsWin32` 0.3.369 | Windows P/Invoke code gen |
| `Fody` 6.9.3 | Compile-time IL weaving |
| `SDL3-CS` 3.4.20 | SDL3 bindings |
| `SharpPdb` 1.0.4 | PDB parsing for native member scanner |
| `xunit.v3` 4.0.2 | Testing |

---

## Mod Development Contract

For mod projects consuming this framework:

### `.csproj` Properties

```xml
<ModName>MyMod</ModName>
<ModType>mod|library|plugin</ModType>
<ModMain>MyMod.MainClass</ModMain>
<AutoInstallMod>true</AutoInstallMod>        <!-- auto-copy to mods/ -->
<GenerateDiffCDB>true</GenerateDiffCDB>      <!-- modify game data (requires GameVersion) -->
<GenerateSinglePakFile>true</GenerateSinglePakFile>  <!-- single res.pak -->
<GameVersion>35</GameVersion>
<NoModCore>true</NoModCore>                  <!-- opt out of default ModCore references -->
```

Extra MSBuild items: `<ModDependency Include="..."/>`, `<ModTag Include="..."/>`, and
`<PackAssets Include="assets/**/*" RootInPak="my_mod"/>` for asset packing.

### `modinfo.json`

`modinfo.json` is generated by the MDK during build (`GenerateModInfo`, default on), merging
`ModName`, `Version`, `ModType`, tags, dependencies, and executable/Pak metadata. Commit a
hand-written `modinfo.json` only as an optional template; it is merged with generated values.

### Entry Point Class

```csharp
public class MainClass(ModInfo info) : ModBase(info), IOnGameInit
{
    public override void Initialize()
    {
        Logger.Information("My mod loaded!");
    }

    void IOnGameInit.OnGameInit() { /* setup */ }
}
```

### NuGet Reference

```xml
<PackageReference Include="DeadCellsCoreModding.MDK" Version="1.0.1" />
```

---

## Common Patterns Quick Reference

| Pattern | How |
|---|---|
| **Module singleton** | `public class Foo : CoreModule<Foo>` → `Foo.Instance` |
| **Event broadcast** | `EventSystem.BroadcastEvent<IOnGameInit>()` |
| **Event with callback** | `EventSystem.BroadcastEvent<ISomeEvent, ISomeEvent.Callback>((a, b) => { ... })` |
| **Find receivers** | `EventSystem.FindReceivers<ModBase>()` |
| **Logging** | `Logger.Information("msg {var}", val)` |
| **Config** | `new Config<MyConfig>("key")` |
| **Save data** | `new SaveData<MyData>("key")` |
| **HL hook** | `HashlinkHooks.Instance.CreateHook("cls", "func", handler).Enable()` |
| **Haxe string** | `"hello".AsHaxeString()` |
| **Thread guard** | `Core.ThrowIfNotMainThread()` |
| **Rethrow across HL** | `ExceptionDispatchInfo.Capture(ex).Throw()` in hook manager |
| **Platform check** | `OperatingSystem.IsWindows()` / `OperatingSystem.IsLinux()` |

---

## Commit Guidelines

### Rules

1. **Use English** for all commit messages
2. **Analyze actual diff content** file by file, group changes by functional module
3. **Carefully verify each module** before committing
4. **Choose the right type** — distinguish between:
   - `feat` — new feature or functionality
   - `fix` — bug fix
   - `chore` — maintenance, dependency updates, non-functional changes
   - `ci` — CI/CD pipeline changes (`.github/workflows/`, build scripts)
   - `refactor` — code restructuring without behavior change
5. **File access scope** is limited to the project root directory and its subdirectories (where `build.ps1` and `build.sh` live)
6. **HashlinkNET.\* projects** are for generating pseudo-code DLLs — note this context in relevant commits
7. **Subject line** should be concise and general, summarizing the change
8. **Body** should briefly summarize what changed in each module

### Commit Message Format

```
<type>: <subject>

<body>
```

Example:

```
feat(ModLoader): add support for nested mod dependencies

- ModLoader: resolve transitive dependencies from modinfo.json
- ModInfo: add optional "dependencies" field parsing
- SampleSimple: update modinfo.json to demonstrate nested deps
```

### Topic vs Body

- **Topic** (`<type>: <subject>`): one-line, 72 chars max, summarizes the entire change
- **Body**: bullet points per module, briefly describing what changed

### Pre-Commit Checklist

Before committing:
- Verify `git status` shows only intended files
- Review `git diff --staged` for unintended changes
- Ensure no secrets or credentials are staged
- Confirm all modified files are within the project root scope

---

## Release Info Guidelines

### Rules

1. **Compare HEAD vs last tag** diff (ignore commit messages), not the commit log
2. **Analyze actual diff content** file by file, group changes by functional module
3. **Classify as Feature or Fix** — carefully distinguish between them:
   - **Feature** — new functionality, enhancements, additions
   - **Fix** — bug fixes, corrections, regressions
4. **Each module gets one sentence** describing the purpose/effect
5. **Do not output file names or specific code changes** — describe at the module/behavior level
6. **File access scope** is limited to the project root directory and its subdirectories (where `build.ps1` and `build.sh` live)
7. **HashlinkNET.\* projects** are for generating pseudo-code DLLs — understand this context
8. **Commit messages are reference only** — a single commit may contain multiple changes; commits and changes may not correspond one-to-one
9. **Remove duplicate entries** — if a change appears in both Feature and Fix, keep it only in one
10. **Must ignore**: CI-related changes, git submodule changes, `latest-release.md` changes
11. **Version number** from `Directory.Build.props` (`<Version>x.x.x</Version>`)
12. **Bilingual output** — generate both English and Chinese versions
13. **Never use double quotes** — use single quotes instead
14. **Check word order** — ensure natural phrasing in both languages
15. **Must include SignPath code signing policy attribution** at the end of both EN and ZH release notes (see template below)

### Output Files

| File | Format |
|---|---|
| `bin/ReleaseInfo.en.md` | `# Release Notes - x.x.x` → `## Feature` → `## Fix` |
| `bin/ReleaseInfo.zh.md` | `# 更新说明` → `## Feature` → `## Fix` |
| `bin/ReleaseInfo.md` | Merged EN + ZH (not mixed; separate sections per language) |
| `bin/ModCoreVersion.txt` | Plain version number: `x.x.x` |

### ReleaseInfo.en.md Format

```
# Release Notes - <version>

## Feature

- ...

## Fix

- ...

---

**Code signing policy**: Free code signing provided by [SignPath.io](https://about.signpath.io), certificate by [SignPath Foundation](https://signpath.org).
```

### ReleaseInfo.zh.md Format

```
# 更新说明

## Feature

- ...

## Fix

- ...

---

**代码签名政策**: 免费代码签名由 [SignPath.io](https://about.signpath.io) 提供，证书由 [SignPath Foundation](https://signpath.org) 颁发。
```

### Merged ReleaseInfo.md

- Keep Chinese and English **separate** (do not mix languages in one section)
- Verify word order is correct in both languages
- Delete or merge duplicate entries
- Unify entry structure while preserving natural phrasing
- Final output is ready for GitHub Release

### Post-Generation Checklist

- [ ] Compared HEAD vs last tag (not commits)
- [ ] Grouped by module, not by file
- [ ] Feature vs Fix correctly classified
- [ ] CI and submodule changes excluded
- [ ] `latest-release.md` changes excluded
- [ ] No double quotes anywhere
- [ ] No duplicate entries across Feature/Fix
- [ ] Bilingual output generated
- [ ] SignPath code signing policy attribution included in both EN and ZH
- [ ] `bin/ModCoreVersion.txt` written with version from `Directory.Build.props`

---

## OpenCode Integration

This project uses OpenCode with custom commands defined in `.opencode/commands/`:

- **`/commit`** — Generates conventional commit messages following the guidelines above, from diff analysis
- **`/release-info`** — Generates bilingual (EN/ZH) release notes comparing HEAD with last tag, excluding CI and submodule changes

---

## Rules for AI Agents

1. **Unsafe code is normal** — This project uses `unsafe` blocks, pointers, and native interop extensively. Do not flag or refactor these.

2. **Primary constructors are preferred** — Use `class Foo(Bar bar) : Base(bar)` syntax, not explicit constructors.

3. **Block-scoped namespaces** — Use `namespace X { ... }`, not file-scoped.

4. **XML-doc all public API** — Every public type, method, property, and interface needs a `<summary>` comment. Use `<inheritdoc/>` for overrides.

5. **Structured logging only** — `Logger.Information("msg {var}", val)`, never string interpolation in log messages.

6. **Event interfaces for lifecycle** — New lifecycle hooks go in `Events/Interfaces/` as interfaces. Modules implement them.

7. **Core modules use `[CoreModule]`** — All built-in framework services use `CoreModule<T>` with the attribute, not `ModBase`.

8. **Match existing priority conventions** — Module priorities are defined in `ModulePriorities.cs`. New modules should slot into the existing ordering.

9. **Never suppress nullable warnings project-wide** — Fix nullable issues, don't disable them.

10. **Tests require full runtime** — Integration tests need the Hashlink VM. Run via `dotnet test`, not standalone.

11. **Build with NUKE, not plain `dotnet build`** — Use `build.ps1` or `build.sh` to invoke the full build pipeline.

12. **Document breaking changes in release notes** — API changes affecting mod authors must be noted.

13. **HashlinkNET.\* generates pseudocode DLLs** — `HashlinkNET.Bytecode` and `HashlinkNET.Compiler` translate game HL bytecode into .NET assemblies; keep this context when changing codegen or proxies.

14. **Single JIT execution path** — The VM runs game bytecode through Hashlink's JIT. Native hooks, marshaling, and proxies target this path.
