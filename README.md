
# Dead Cells Core Modding (WIP)

A Dead Cells Modding API/loader. 

## Development

### Requirement

- .NET 9 SDK
- CMake

### Build

#### Windows

1. Clone the repository
2. Run `buildWin.bat`

### Install

1. After "Build", copy the `bin` folder in the root directory of the repository to `<DeadCellsGameRoot>` and rename it to `coremod`
2. Start the game from `<DeadCellsGameRoot>/coremod/core/host/startup/DeadCellsModding.exe`

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

## License

Distributed under the MIT [license](https://github.com/DreamBoxSpy/DeadCellsCoreModding/blob/main/LICENSE).
