

add_library (tcc SHARED tinycc.c)
target_include_directories(tcc PRIVATE "./include")
SET_TARGET_PROPERTIES(tcc PROPERTIES 
        RUNTIME_OUTPUT_DIRECTORY ${DCCM_OUTPUT_DIR}
        LIBRARY_OUTPUT_DIRECTORY ${DCCM_OUTPUT_DIR}
        CXX_STANDARD 17
        CXX_STANDARD_REQUIRED YES
        CXX_EXTENSIONS NO
        OUTPUT_NAME "libtcc"
        PREFIX "")
