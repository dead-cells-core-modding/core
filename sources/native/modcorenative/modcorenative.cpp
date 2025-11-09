#define NETHOST_USE_AS_STATIC
#include <hostfxr.h>
#include <nethost.h>
#include <coreclr_delegates.h>

#include <stdlib.h>
#include <stdio.h>

#include <filesystem>
#include <string>

#define DCCM_NATIVE_LIB
#include "modcorenative.h"

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

static tagDCCM_MAPIS managed_apis;

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

DCCM_API DCCM_API_INFOS get_managed_api_info() {
    memset(&managed_apis, 0, sizeof(DCCM_MAPIS));
    
    int apisCount = 0;
#define DCCM_MANAGED_API(name, ret, ...) apisCount++;
#include "managed_apis.h"
#undef DCCM_MANAGED_API
    
    DCCM_API_INFOS info = {};
    info.count = apisCount;
    info.names = (const char**) new char* [apisCount];
    info.ptr = new void* [apisCount];

    int apiIndex = 0;

#define DCCM_MANAGED_API(name, ret, ...) info.names[apiIndex] = ""#name; info.ptr[apiIndex++] = &managed_apis.##name;
#include "managed_apis.h"
#undef DCCM_MANAGED_API

    return info;
}

DCCM_API PDCCM_MAPIS try_load_dccm(char_t* gamePath, const void** args, int argc, const char** err) {
    if (!load_hostfxr()) {
        *err = "Unable to load hostfxr";
        return nullptr;
    }
    std::filesystem::path gameRoot(gamePath);
    gameRoot /= "coremod";
    gameRoot /= "core";
    gameRoot /= "host";
    auto hostRoot(gameRoot);

    gameRoot /= "DCCMShell.runtimeconfig.json";
    load_assembly_and_get_function_pointer_fn gfp = get_dotnet_load_assembly(gameRoot.wstring().c_str());
    if (gfp == nullptr) {
        *err = "Unable to initialize coreclr";
        return nullptr;
    }
    hostRoot /= "DCCMShell.dll";
    component_entry_point_fn entry = nullptr;
    if (
        gfp(hostRoot.wstring().c_str(), L"DCCMShell.Shell,DCCMShell", L"StartFromNative", nullptr, nullptr, (void**)&entry)
        ) {
        *err = "Unable to find entry point";
        return nullptr;
    }

    DCCM_API_INFOS info = get_managed_api_info();

    struct {
        const char** err;
        int argc;
        const void** args;
        DCCM_API_INFOS* api_infos;
    } in_args = {
            err,
            argc,
            args,
            &info
    };
    entry(&in_args, sizeof(in_args));
    return 0;
}

