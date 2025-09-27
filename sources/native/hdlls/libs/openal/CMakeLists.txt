if(WIN32)
    if(MSVC)
        set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /SAFESEH:NO")
    endif()

    if(CMAKE_SIZEOF_VOID_P EQUAL 8)
        find_library(OpenAL_LIBRARY OpenAL32 PATHS ${INCLUDES_BASE_DIR}/openal/libs/Win64)
        set(OpenAL_LIBRARY_DLL ${INCLUDES_BASE_DIR}/openal/bin/Win64/soft_oal.dll)
    else()
        find_library(OpenAL_LIBRARY OpenAL32 PATHS ${INCLUDES_BASE_DIR}/openal/libs/Win32)
        set(OpenAL_LIBRARY_DLL ${INCLUDES_BASE_DIR}/openal/bin/Win32/soft_oal.dll)
    endif()

    add_library(openal.hdll SHARED openal.c)

    set_as_hdll(openal)

    target_include_directories(openal.hdll
        PRIVATE
        ${INCLUDES_BASE_DIR}/openal/include
    )

    target_link_libraries(openal.hdll
        libhl
        ${OpenAL_LIBRARY}
    )

    add_custom_command(
    TARGET openal.hdll POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy
            ${OpenAL_LIBRARY_DLL}
            ${OUTPUT_DIR}/OpenAL32.dll
    )

    install(
        TARGETS
            openal.hdll
        DESTINATION ${HDLL_DESTINATION}
    )
endif()
