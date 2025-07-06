

include_directories (../3rd/hashlink/src)
file(GLOB SOURCES ./*.c)

remove_definitions(-DLIBHL_EXPORTS)

if(WIN32)
set(ASM_HELPER_SRC ./win_x64.asm)
else()
set(ASM_HELPER_SRC ./linux_x64.asm)
endif()

add_library (modcorenative SHARED ${ASM_HELPER_SRC} ${SOURCES} ${PLATFORM_SOURCES})



if (WIN32)
target_link_libraries(modcorenative dbghelp)
endif()

target_link_libraries(modcorenative libhl)

install(TARGETS modcorenative)

SET_TARGET_PROPERTIES(modcorenative PROPERTIES LINKER_LANGUAGE C
        RUNTIME_OUTPUT_DIRECTORY ${CMAKE_RUNTIME_OUTPUT_DIRECTORY}
        LIBRARY_OUTPUT_DIRECTORY ${CMAKE_RUNTIME_OUTPUT_DIRECTORY}
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
