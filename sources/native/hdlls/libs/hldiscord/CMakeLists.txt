if(WIN32)
    if(MSVC)
        set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /SAFESEH:NO")
    endif()

    get_filename_component(Discord_LIBRARY ${CMAKE_CURRENT_SOURCE_DIR}/lib/amd64/discord-rpc.lib ABSOLUTE)

    add_library(discord.hdll SHARED src/hldiscord.cpp)

    set_as_hdll(discord)

    target_include_directories(discord.hdll
        PRIVATE
        ./include/discord
    )

    target_link_libraries(discord.hdll
        libhl
        ${Discord_LIBRARY}
    )

    install(
        TARGETS
            discord.hdll
        DESTINATION ${HDLL_DESTINATION}
    )
endif()
