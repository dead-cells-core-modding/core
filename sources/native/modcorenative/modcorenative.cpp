#define NETHOST_USE_AS_STATIC
#include <hostfxr.h>
#include <nethost.h>
#include <coreclr_delegates.h>

#define DCCM_NATIVE_LIB
#include "modcorenative.h"

#include <stdlib.h>
#include <stdio.h>

#include <filesystem>
#include <string>

#if WIN32
#define load_library(path) LoadLibraryW(path)
#define get_export(handle, name) GetProcAddress((HMODULE)handle, name)
#include <Windows.h>
#endif

hostfxr_initialize_for_dotnet_command_line_fn init_for_cmd_line_fptr;
hostfxr_initialize_for_runtime_config_fn init_for_config_fptr;
hostfxr_get_runtime_delegate_fn get_delegate_fptr;
hostfxr_run_app_fn run_app_fptr;
hostfxr_close_fn close_fptr;

int load_hostfxr() {
    char_t buffer[MAX_PATH];
    size_t buffer_size = sizeof(buffer) / sizeof(char_t);
    int rc = get_hostfxr_path(buffer, &buffer_size, NULL); 
    void* lib = load_library(buffer);
    if (rc != 0 || lib == 0)
        return 0;

    
    init_for_config_fptr = (hostfxr_initialize_for_runtime_config_fn)get_export(lib, "hostfxr_initialize_for_runtime_config");
    get_delegate_fptr = (hostfxr_get_runtime_delegate_fn)get_export(lib, "hostfxr_get_runtime_delegate");
    close_fptr = (hostfxr_close_fn)get_export(lib, "hostfxr_close");

    return (init_for_config_fptr && get_delegate_fptr && close_fptr);
}

load_assembly_and_get_function_pointer_fn get_dotnet_load_assembly(const char_t* config_path)
{
    void* load_assembly_and_get_function_pointer = 0;
    hostfxr_handle cxt = 0;
    int rc = init_for_config_fptr(config_path, 0, &cxt);
    if (rc != 0 || cxt == 0)
    {
        close_fptr(cxt);
        return 0;
    }

    // Get the load assembly function pointer
    rc = get_delegate_fptr(
        cxt,
        hdt_load_assembly_and_get_function_pointer,
        &load_assembly_and_get_function_pointer);

    close_fptr(cxt);
    return (load_assembly_and_get_function_pointer_fn)load_assembly_and_get_function_pointer;
}

DCCM_API int try_load_dccm(char_t* gamePath) {
    if (!load_hostfxr()) {
        return -1;
    }
    std::filesystem::path gameRoot(gamePath);
    gameRoot /= "coremod";
    gameRoot /= "core";
    gameRoot /= "host";
    auto hostRoot(gameRoot);

    gameRoot /= "DCCMShell.runtimeconfig.json";
    load_assembly_and_get_function_pointer_fn gfp = get_dotnet_load_assembly(gameRoot.wstring().c_str());
    if (gfp == nullptr) {
        return -2;
    }
    hostRoot /= "DCCMShell.dll";
    component_entry_point_fn entry = nullptr;
    if (
        gfp(hostRoot.wstring().c_str(), L"DCCMShell.Shell,DCCMShell", L"StartFromNative", nullptr, nullptr, (void**)&entry)
        ) {
        return -3;
    }

    const wchar_t* args[] = {
        L""
    };
    entry(args, sizeof(args));
}

