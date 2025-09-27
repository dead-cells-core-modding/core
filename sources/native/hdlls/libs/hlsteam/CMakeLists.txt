
    if(MSVC)
        set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /SAFESEH:NO")
    endif()
 
    file(GLOB SOURCES ./native/*.cpp)

    get_filename_component(STEAMSDK_ROOT "../../include/Steamworks-SDK" ABSOLUTE)

    find_library(Steam_LIBRARY steam_api64 PATHS ${STEAMSDK_ROOT}/redistributable_bin/win64)

    add_library(steam.hdll SHARED ${SOURCES})

    set_as_hdll(steam)

    target_include_directories(steam.hdll
        PRIVATE
        ${STEAMSDK_ROOT}/public
        PRIVATE 
        ./native
    )

    target_link_libraries(steam.hdll
        libhl
        ${Steam_LIBRARY}
    )
    set(STEAM_DLL
        "${STEAMSDK_ROOT}/redistributable_bin/win64/steam_api64.dll"
    )
    add_custom_command(
                TARGET steam.hdll POST_BUILD
                COMMAND ${CMAKE_COMMAND} -E copy_if_different
                    ${STEAM_DLL}
                    ${CMAKE_RUNTIME_OUTPUT_DIRECTORY}/
    )

