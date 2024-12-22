

include_directories (../include)
file(GLOB_RECURSE SOURCES ./*.c ./*.cpp)

if(X86)
set(ASM_HELPER_SRC ./asm_x86.asm)
else()
set(ASM_HELPER_SRC ./asm_x64.asm)
endif()

remove_definitions(-DLIBHL_EXPORTS)

add_library (modcorenative SHARED ${SOURCES} ${ASM_HELPER_SRC})

if (WIN32)
set(LIB_ROOT ../lib/win-x86/)
file(GLOB_RECURSE LIBS ${LIB_ROOT}*.lib)


else()

set(LIB_ROOT ../lib/linux-x64/)
file(GLOB_RECURSE LIBS ${LIB_ROOT}*.a)
endif()



target_link_libraries(modcorenative ${LIBS} libhl)

install(TARGETS modcorenative)

SET_TARGET_PROPERTIES(modcorenative PROPERTIES LINKER_LANGUAGE C
        RUNTIME_OUTPUT_DIRECTORY ${CMAKE_LIBRARY_OUTPUT_DIRECTORY}
        LIBRARY_OUTPUT_DIRECTORY ${CMAKE_LIBRARY_OUTPUT_DIRECTORY}
        OUTPUT_NAME "modcorenative"
        PREFIX "")

