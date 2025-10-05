
get_filename_component(INCLUDES_BASE_DIR "${CMAKE_CURRENT_SOURCE_DIR}/../include" ABSOLUTE)

function(set_as_hdll target)
    set_target_properties(${target}.hdll
        PROPERTIES
        PREFIX ""
        OUTPUT_NAME ${target}
        SUFFIX .hdll
    )
endfunction()

add_subdirectory(directx)
add_subdirectory(fmt)
add_subdirectory(openal)
add_subdirectory(sdl)
add_subdirectory(sqlite)
add_subdirectory(ssl)
add_subdirectory(ui)
add_subdirectory(uv)
add_subdirectory(hldiscord)
add_subdirectory(hlsteam)
add_subdirectory(hlchroma)
