

include_directories (../3rd/hashlink/src)
file(GLOB SOURCES ./*.c)

remove_definitions(-DLIBHL_EXPORTS)



set(HL_SRC_ROOT ../3rd/hashlink/src)

file(GLOB hl_srcs 
        ${HL_SRC_ROOT}/code.c
        ${HL_SRC_ROOT}/jit.c
        ${HL_SRC_ROOT}/main.c
        ${HL_SRC_ROOT}/module.c
        ${HL_SRC_ROOT}/debugger.c
        ${HL_SRC_ROOT}/profile.c)


add_library (modcorenative SHARED ${ASM_HELPER_SRC} ${SOURCES} ${PLATFORM_SOURCES} ${hl_srcs})

if (WIN32)
target_link_libraries(modcorenative dbghelp)
endif()

target_link_libraries(modcorenative libhl)


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

#Copy libhl
add_custom_command(TARGET modcorenative POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy_directory
            "${LIBHL_OUTPUT_DIR}"
            "${CMAKE_RUNTIME_OUTPUT_DIRECTORY}"
)
