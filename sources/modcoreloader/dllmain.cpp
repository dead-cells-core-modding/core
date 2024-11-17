
#include "pch.h"
#include <nethost.h>
#include <hostfxr.h>
#include <coreclr_delegates.h>

#define MODCORE_FILE(name) L"coremod/core/host/" name

void DECLSPEC_NORETURN fail_to_load()
{
    ExitProcess(-1);
}

load_assembly_and_get_function_pointer_fn load_coreclr()
{
    printf_s("Loading coreclr\n");

    char_t hostfxr_path[MAX_PATH];
    size_t hostfxr_path_size = sizeof(hostfxr_path) / sizeof(char_t);
    if (get_hostfxr_path(hostfxr_path, &hostfxr_path_size, nullptr) != 0)
    {
        printf_s("Failed to get hostfxr path");
        fail_to_load();
    }
    wprintf_s(L"Load hostfxr from %s\n", hostfxr_path);

    HMODULE hHostfxr = LoadLibrary(hostfxr_path);
    if (hHostfxr == NULL)
    {
        printf_s("Failed to load hostfxr\n");
        fail_to_load();
    }
    auto init_fptr = (hostfxr_initialize_for_runtime_config_fn)GetProcAddress(hHostfxr, "hostfxr_initialize_for_runtime_config");
    auto get_delegate_fptr = (hostfxr_get_runtime_delegate_fn)GetProcAddress(hHostfxr, "hostfxr_get_runtime_delegate");
    auto close_fptr = (hostfxr_close_fn)GetProcAddress(hHostfxr, "hostfxr_close");

    hostfxr_handle cxt = nullptr;
    int32_t result = init_fptr(MODCORE_FILE("ModCore.runtimeconfig.json"), nullptr, &cxt);
    if (result != 0) {
        printf_s("Failed to init_fptr: %x\n", result);
        close_fptr(cxt);
        fail_to_load();
    }

    load_assembly_and_get_function_pointer_fn load_assembly_and_get_function_pointer{};
    result = get_delegate_fptr(cxt, hdt_load_assembly_and_get_function_pointer, (void**) & load_assembly_and_get_function_pointer);
    if (result != 0) {
        printf_s("Failed to get_delegate_fptr: %x\n", result);
        close_fptr(cxt);
        fail_to_load();
    }
    close_fptr(cxt);

    return load_assembly_and_get_function_pointer;
}

void load_modcore()
{
    printf_s("Loading Modding Core\n");
    auto load_assembly_and_get_function_pointer = load_coreclr();
    
    component_entry_point_fn inject_main_ptr{};
    int32_t result = load_assembly_and_get_function_pointer(MODCORE_FILE("ModCore.dll"),
        L"ModCore.Program, ModCore", L"InjectMain", nullptr, nullptr, (void**) & inject_main_ptr);
    if (result != 0) {
        printf_s("Failed to load_assembly_and_get_function_pointer: %x\n", result);
        fail_to_load();
    }
    inject_main_ptr(nullptr, 0);
}

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        load_modcore();
        break;
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

