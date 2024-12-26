
include_directories (../include)

file(GLOB_RECURSE SOURCES ./*.c ./*.cpp ./*.h)


add_library(libhl SHARED ${SOURCES})

install(TARGETS libhl)

set_target_properties(libhl
        PROPERTIES
        OUTPUT_NAME libhl
)

set_target_properties(libhl
    PROPERTIES
    COMPILE_DEFINITIONS "_USRDLL;LIBHL_EXPORTS"
)

SET_TARGET_PROPERTIES(libhl PROPERTIES LINKER_LANGUAGE C
        RUNTIME_OUTPUT_DIRECTORY ${CMAKE_LIBRARY_OUTPUT_DIRECTORY}
        LIBRARY_OUTPUT_DIRECTORY ${CMAKE_LIBRARY_OUTPUT_DIRECTORY}
        PREFIX "")

if(WIN32)
    target_link_libraries(libhl ws2_32 user32)
else()
    target_link_libraries(libhl m dl pthread)
endif()