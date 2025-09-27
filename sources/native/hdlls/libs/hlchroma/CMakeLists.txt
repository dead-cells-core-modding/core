cmake_minimum_required(VERSION 3.10)

add_library(chroma.hdll SHARED src/hlchroma.cpp)

set_as_hdll(chroma)

target_include_directories(chroma.hdll PUBLIC
    "${CMAKE_CURRENT_SOURCE_DIR}/Include"
    "${HASHLINK_SRC}/src"
)

target_link_libraries(chroma.hdll libhl)

