

include_directories (../3rd/hashlink/src)
file(GLOB SOURCES ./*.c)

if(WIN32)
set(ASM_HELPER_SRC ./win_x64.asm)
else()
set(ASM_HELPER_SRC ./linux_x64.asm)
endif()

remove_definitions(-DLIBHL_EXPORTS)


add_library (modcorenative SHARED ${SOURCES} ${ASM_HELPER_SRC} ${PLATFORM_SOURCES})

if (WIN32)
target_link_libraries(modcorenative dbghelp)
endif()

target_link_libraries(modcorenative libhl)

install(TARGETS modcorenative)

SET_TARGET_PROPERTIES(modcorenative PROPERTIES LINKER_LANGUAGE C
        RUNTIME_OUTPUT_DIRECTORY ${CMAKE_LIBRARY_OUTPUT_DIRECTORY}
        LIBRARY_OUTPUT_DIRECTORY ${CMAKE_LIBRARY_OUTPUT_DIRECTORY}
        OUTPUT_NAME "modcorenative"
        PREFIX "")

