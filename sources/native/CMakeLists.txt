
cmake_minimum_required (VERSION 3.20)

project ("native")

SET(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${PROJECT_SOURCE_DIR}/../../bin/core/native/)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_C_STANDARD 11)


# ---- Target architecture selection ----
# TARGET_ARCH is provided by the CMake presets (x64 today; arm64 reserved for
# future Android arm64 support). Fall back to the host processor when unset.
if(NOT DEFINED TARGET_ARCH)
    string(TOLOWER "${CMAKE_SYSTEM_PROCESSOR}" _host_arch_lower)
    if(_host_arch_lower MATCHES "arm64|aarch64")
        set(TARGET_ARCH "arm64")
    else()
        set(TARGET_ARCH "x64")
    endif()
endif()

string(TOLOWER "${TARGET_ARCH}" _target_arch_lower)

if(_target_arch_lower MATCHES "arm64|aarch64")
    set(ARCH_NAME arm64)
    set(DCCM_ARCH_ARM64 true)
    add_definitions(-DARM64)
elseif(_target_arch_lower MATCHES "amd64|x86_64|x64")
    set(ARCH_NAME x64)
    set(DCCM_ARCH_X64 true)
    add_definitions(-DX64)
else()
    message(FATAL_ERROR "Unsupported TARGET_ARCH: ${TARGET_ARCH}")
endif()

if(WIN32)
set(DCCM_OUTPUT_DIR ${CMAKE_RUNTIME_OUTPUT_DIRECTORY}win-${ARCH_NAME})
add_definitions(-DJIT_CUSTOM_LONGJUMP)
elseif(ANDROID)
set(DCCM_OUTPUT_DIR ${CMAKE_RUNTIME_OUTPUT_DIRECTORY}android-${ARCH_NAME})
else()
set(DCCM_OUTPUT_DIR ${CMAKE_RUNTIME_OUTPUT_DIRECTORY}linux-${ARCH_NAME})
endif()

cmake_host_system_information(RESULT CPU_NUMBER_OF_LOGICAL_CORES QUERY NUMBER_OF_LOGICAL_CORES)
add_compile_options($<$<CXX_COMPILER_ID:MSVC>:/MP${CPU_NUMBER_OF_LOGICAL_CORES}>)

if(MSVC)

set(WINDOWS_EXPORT_ALL_SYMBOLS true)
set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreadedDLL")

add_definitions(-D_CRT_SECURE_NO_WARNINGS=1 -DUNICODE -D_UNICODE)
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /Zi /MP${CPU_NUMBER_OF_LOGICAL_CORES}")
set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} /Zi /MP${CPU_NUMBER_OF_LOGICAL_CORES}")

add_compile_options(/Ob0 /INCREMENTAL:NO)

set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /DEBUG /OPT:ICF")
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /DEBUG /OPT:ICF")

else()

add_compile_options(-fno-inline -fno-inline-functions-called-once -fno-strict-aliasing -fno-delete-null-pointer-checks -fno-omit-frame-pointer -fno-optimize-sibling-calls)

set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -Wno-incompatible-pointer-types -Wno-int-conversion")

endif()

set(BUILD_TESTING off)

set(WITH_DX12 off)
set(WITH_UV off)
set(WITH_UI off)
set(WITH_SSL off)
set(WITH_SQLITE off)
set(WITH_SDL off)
set(WITH_OPENAL off)
set(WITH_FMT off)
set(WITH_DIRECTX off)
set(WITH_VIDEO off)

 
    list(APPEND CMAKE_MODULE_PATH
        ${CMAKE_CURRENT_SOURCE_DIR}/hdlls/other/cmake
    )

    set(BUILD_SHARED_LIBS on)
    
    add_subdirectory ("3rd/hashlink")
    add_subdirectory ("3rd/tinycc")

    include_directories("3rd/hashlink/src")
    
    get_filename_component(HDLLS_INCLUDE "3rd/hashlink/include" ABSOLUTE)
    
    get_target_property(LIBHL_OUTPUT_DIR libhl RUNTIME_OUTPUT_DIRECTORY)
    
    set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${DCCM_OUTPUT_DIR})
    set(OUTPUT_DIR ${CMAKE_RUNTIME_OUTPUT_DIRECTORY})
    
    foreach(OUTPUTCONFIG ${CMAKE_CONFIGURATION_TYPES})
        string(TOUPPER ${OUTPUTCONFIG} OUTPUTCONFIG_UPPER)
        set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_${OUTPUTCONFIG_UPPER} ${CMAKE_RUNTIME_OUTPUT_DIRECTORY})
        set(CMAKE_LIBRARY_OUTPUT_DIRECTORY_${OUTPUTCONFIG_UPPER} ${CMAKE_RUNTIME_OUTPUT_DIRECTORY})
        #set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY_${OUTPUTCONFIG_UPPER} ${CMAKE_RUNTIME_OUTPUT_DIRECTORY})
    endforeach()

add_subdirectory ("hdlls/libs")
add_subdirectory ("modcorenative")
add_subdirectory ("tinycc")

if(WIN32)

add_subdirectory ("host_lib")
add_subdirectory ("cv2pdb")

endif()
