
# Release Notes - 35.11.3

## Feature

- Full Linux platform native support: implemented all previously unimplemented Linux native methods including TLS thread-local storage, memory page protection, stack frame management, HL↔C# context switching via generated assembly code, HL boot data extraction, and more — the game is now playable on Linux
- Steam Workshop automated publishing pipeline: new build targets to download CI artifacts and auto-package/publish to Steam Workshop for both win-x64 and linux-x64 platforms
- CI/CD extended to Linux platform: GitHub Actions now enables Ubuntu build and test workflows, with automatic crash dump upload on test failure
- Pseudocode compiler exception handling infrastructure: added Trap/EndTrap/Catch opcode parsing and exception handler generation steps for the pseudocode DLL, supporting try-catch region detection and IL exception handler creation
- Hashlink Catch opcode support: new Catch opcode added to the bytecode parser for exception type filtering
- Platform architecture identification: PlatformServices now exposes a Name property for win-x64/linux-x64 path resolution
- Goldberg Steam emulator updated to latest version

## Fix

- Fix Linux crash due to wrong TLS slot: trap_magic_number now reads from a dynamically allocated memory address instead of a hardcoded value, resolving jmp_buf struct size and trap_ctx offset mismatch issues that caused assertion failures
- Fix break_on_trap hook fallback stack leak: the fallback path now correctly cleans up the stack and returns zero, instead of leaking arguments by jumping to the original function
- Fix libhl native library loading path on Linux: prioritizes libhl.so.1 loading and correctly reads the link_map first field to obtain the actual load base address for proper symbol offset resolution
- Fix Linux jmp_buf struct size mismatch: added platform-conditional compilation — glibc jmp_buf is 200 bytes, previously incorrectly used the Windows size of 256 bytes
- Fix game crash on Steam platform module initialization: added try-catch error handling for Steam module init and callback invocations
- Fix Steam API native library resolution on Linux: loads libsteam_api.so from the native directory to work with the Goldberg emulator
- Fix test runner crash when DEAD_CELLS_GAME_PATH is not set: replaced null reference crash with a clear error message
- Fix CMake compiler flags: removed -fno-inline and replaced with -fno-inline-functions-called-once to properly disable inline compilation
- Fix release workflow push order: commit now executes before tag creation to ensure correct version release flow
- Fix Steam Workshop content directory structure: adapted for win-x64/linux-x64 subdirectories to ensure mod content deploys to the correct platform path
