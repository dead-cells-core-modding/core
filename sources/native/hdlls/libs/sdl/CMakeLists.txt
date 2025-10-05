
if(WIN32)
    set(SDL2_PATH ${HDLLS_INCLUDE}/sdl)
    set(SDL2_INCLUDE_DIR ${SDL2_PATH}/include)

    find_library(SDL2_LIBRARY SDL2 PATHS ${SDL2_PATH}/lib/x64)
endif()

add_library(sdl.hdll SHARED
    sdl.c
    gl.c
)
set_as_hdll(sdl)

message("SDL include: ${SDL2_INCLUDE_DIR}")

target_include_directories(sdl.hdll
    PRIVATE
    ${SDL2_INCLUDE_DIR}
    ${INCLUDES_BASE_DIR}/gl
)

target_link_libraries(sdl.hdll
    libhl
    ${SDL2_LIBRARY}
)

if(WIN32)
    target_link_libraries(sdl.hdll
        winmm
        opengl32
    )
endif()

if(APPLE)
    find_package(OPENGL REQUIRED)
    target_include_directories(sdl.hdll
        PRIVATE
        ${OPENGL_INCLUDE_DIR}
    )
    target_link_libraries(sdl.hdll
        libhl
        ${OPENGL_gl_LIBRARY}
    )
endif()

if(WIN32)
set(SDL2_LIBRARY_DLL ${SDL2_PATH}/lib/x64/SDL2.dll)
add_custom_command(
    TARGET sdl.hdll POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
            ${SDL2_LIBRARY_DLL}
            ${OUTPUT_DIR}/SDL2.dll
)
endif()

install(
    TARGETS sdl.hdll
    DESTINATION ${HDLL_DESTINATION}
)
