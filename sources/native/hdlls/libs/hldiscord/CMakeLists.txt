if(WIN32)
    if(MSVC)
        set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /SAFESEH:NO")
    endif()

    find_library(Discord_LIBRARY Discord-RPC PATHS ./lib/amd64)

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
