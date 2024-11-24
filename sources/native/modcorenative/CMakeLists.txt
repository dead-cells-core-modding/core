
SET(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${PROJECT_SOURCE_DIR}/bin)

include_directories (../include)

add_library (modcorenative SHARED "modcorenative.cpp" "modcorenative.h")

if (MSVC)
get_filename_component(pnethost ../lib/libnethost.lib ABSOLUTE)
set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} /MT")
else()
get_filename_component(pnethost ../lib/libnethost.a ABSOLUTE)
endif()



target_link_libraries(modcorenative ${pnethost})

set(CMAKE_GENERATOR_PLATFORM x86)
install(TARGETS modcorenative)

SET_TARGET_PROPERTIES(modcorenative PROPERTIES LINKER_LANGUAGE C
        RUNTIME_OUTPUT_DIRECTORY ${CMAKE_LIBRARY_OUTPUT_DIRECTORY}
        LIBRARY_OUTPUT_DIRECTORY ${CMAKE_LIBRARY_OUTPUT_DIRECTORY}
        OUTPUT_NAME "modcorenative"
        PREFIX "")

if (CMAKE_VERSION VERSION_GREATER 3.12)
  set_property(TARGET modcorenative PROPERTY CXX_STANDARD 20)
endif()


