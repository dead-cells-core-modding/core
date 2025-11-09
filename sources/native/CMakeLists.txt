
cmake_minimum_required (VERSION 3.20)

project ("native")

SET(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${PROJECT_SOURCE_DIR}/../../bin/core/native/)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_C_STANDARD 11)


set(X64 true)
set(ARCH_NAME x64)

if(WIN32)
set(DCCM_OUTPUT_DIR ${CMAKE_RUNTIME_OUTPUT_DIRECTORY}win-${ARCH_NAME})
else()
set(DCCM_OUTPUT_DIR ${CMAKE_RUNTIME_OUTPUT_DIRECTORY}linux-${ARCH_NAME})
endif()

add_definitions(-DX64)

cmake_host_system_information(RESULT CPU_NUMBER_OF_LOGICAL_CORES QUERY NUMBER_OF_LOGICAL_CORES)
add_compile_options($<$<CXX_COMPILER_ID:MSVC>:/MP${CPU_NUMBER_OF_LOGICAL_CORES}>)

if(MSVC)

set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreadedDLL")

add_definitions(-D_CRT_SECURE_NO_WARNINGS=1 -DUNICODE -D_UNICODE)
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /Zi /MP${CPU_NUMBER_OF_LOGICAL_CORES}")
set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} /Zi /MP${CPU_NUMBER_OF_LOGICAL_CORES}")

add_compile_options(/Ob0 /INCREMENTAL:NO /HIGHENTROPYVA /DYNAMICBASE)

set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /DEBUG /OPT:ICF")
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /DEBUG /OPT:ICF")

endif()

set(BUILD_TESTING off)

set(WITH_UV off)
set(WITH_UI off)
set(WITH_SSL off)
set(WITH_SQLITE off)
set(WITH_SDL off)
set(WITH_OPENAL off)
set(WITH_FMT off)
set(WITH_DIRECTX off)
set(WITH_VIDEO off)

# Compile native libraries only on Windows platforms
if(WIN32)
    
    list(APPEND CMAKE_MODULE_PATH
        ${CMAKE_CURRENT_SOURCE_DIR}/hdlls/other/cmake
    )
    
    add_subdirectory ("3rd/hashlink")
    
    include_directories("3rd/hashlink/src")
    
    get_filename_component(HDLLS_INCLUDE "3rd/hashlink/include" ABSOLUTE)
    
    set(LIBHL_OUTPUT_DIR $<TARGET_FILE_DIR:libhl>)
    
    set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${DCCM_OUTPUT_DIR})
    set(OUTPUT_DIR ${CMAKE_RUNTIME_OUTPUT_DIRECTORY})
    
    foreach(OUTPUTCONFIG ${CMAKE_CONFIGURATION_TYPES})
        string(TOUPPER ${OUTPUTCONFIG} OUTPUTCONFIG_UPPER)
        set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_${OUTPUTCONFIG_UPPER} ${CMAKE_RUNTIME_OUTPUT_DIRECTORY})
        set(CMAKE_LIBRARY_OUTPUT_DIRECTORY_${OUTPUTCONFIG_UPPER} ${CMAKE_RUNTIME_OUTPUT_DIRECTORY})
        #set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY_${OUTPUTCONFIG_UPPER} ${CMAKE_RUNTIME_OUTPUT_DIRECTORY})
    endforeach()

endif()

add_subdirectory ("hdlls/libs")
add_subdirectory ("modcorenative")
