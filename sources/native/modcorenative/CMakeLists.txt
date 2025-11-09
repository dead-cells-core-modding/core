
include_directories (./includes)
file(GLOB SOURCES ./*.c ./*.cpp)

remove_definitions(-DLIBHL_EXPORTS)

if(WIN32)
    set(HL_SRC_ROOT ../3rd/hashlink/src)
    file(GLOB hl_srcs 
        ${HL_SRC_ROOT}/code.c
        ${HL_SRC_ROOT}/jit.c
        ${HL_SRC_ROOT}/main.c
        ${HL_SRC_ROOT}/module.c
        ${HL_SRC_ROOT}/debugger.c
        ${HL_SRC_ROOT}/profile.c)
    add_library(hljit SHARED  ${hl_srcs})
    target_link_libraries(hljit libhl)
endif()

add_library (modcorenative SHARED ${SOURCES})

target_compile_features(modcorenative PRIVATE cxx_std_20)

# Find nethost
if(WIN32)
set(NETHOST_PACK_NAME "microsoft.netcore.app.host.win-x64")
set(NETHOST_PLATFORM "win-x64")
else()
set(NETHOST_PACK_NAME "microsoft.netcore.app.host.linux-x64")
set(NETHOST_PLATFORM "linux-x64")
endif()

execute_process(
    COMMAND dotnet --info
    OUTPUT_VARIABLE DOTNET_INFO
    OUTPUT_STRIP_TRAILING_WHITESPACE
)
execute_process(
    COMMAND dotnet --version
    OUTPUT_VARIABLE DOTNET_VERSION
    OUTPUT_STRIP_TRAILING_WHITESPACE
)
string(REGEX MATCH "Base Path: ([^\n\r]+)" _match "${DOTNET_INFO}")
string(STRIP  "${CMAKE_MATCH_1}" DOTNET_SDK_ROOT)
set(NETHOST_PACK_VERSIONS_PATH "${DOTNET_SDK_ROOT}../../packs/${NETHOST_PACK_NAME}")
file(GLOB NETHOST_PACK_VERSIONS "${NETHOST_PACK_VERSIONS_PATH}/*")
list(SORT NETHOST_PACK_VERSIONS ORDER DESCENDING)
list(GET NETHOST_PACK_VERSIONS 0 NETHOST_PACK_PATH)

set(NETHOST_PACK_PATH "${NETHOST_PACK_PATH}/runtimes/${NETHOST_PLATFORM}/native")

target_include_directories(modcorenative PRIVATE ${NETHOST_PACK_PATH})
find_library(NetHost_LIBRARY libnethost PATHS ${NETHOST_PACK_PATH})
target_link_libraries(modcorenative ${NetHost_LIBRARY})
message(${NetHost_LIBRARY})

SET_TARGET_PROPERTIES(modcorenative PROPERTIES 
        RUNTIME_OUTPUT_DIRECTORY ${CMAKE_RUNTIME_OUTPUT_DIRECTORY}
        LIBRARY_OUTPUT_DIRECTORY ${CMAKE_RUNTIME_OUTPUT_DIRECTORY}
        CXX_STANDARD 17
        CXX_STANDARD_REQUIRED YES
        CXX_EXTENSIONS NO
        MSVC_RUNTIME_LIBRARY "MultiThreaded"
        OUTPUT_NAME "modcorenative"
        PREFIX "")

#Copy vcruntime
if(MSVC)
    get_filename_component(MSVC_REDIST_DIR "${CMAKE_CXX_COMPILER}" DIRECTORY)
    set(VC_REDIST_DLLS
        "${MSVC_REDIST_DIR}/vcruntime140.dll"
        "${MSVC_REDIST_DIR}/vcruntime140_1.dll"
        "${MSVC_REDIST_DIR}/msvcp140.dll"
        "${MSVC_REDIST_DIR}/concrt140.dll"
    )
    foreach(DLL ${VC_REDIST_DLLS})
        if(EXISTS ${DLL})
            add_custom_command(
                TARGET modcorenative POST_BUILD
                COMMAND ${CMAKE_COMMAND} -E copy_if_different
                    ${DLL}
                    ${CMAKE_RUNTIME_OUTPUT_DIRECTORY}/
            )
        endif()
    endforeach()
endif()

#Copy libhl
add_custom_command(TARGET modcorenative POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy_directory
            "${LIBHL_OUTPUT_DIR}"
            "${CMAKE_RUNTIME_OUTPUT_DIRECTORY}"
)
