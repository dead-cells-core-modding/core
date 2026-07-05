cmake_minimum_required(VERSION 3.20)

project(cv2pdb LANGUAGES CXX)

if(NOT WIN32)
    message(FATAL_ERROR "cv2pdb only supports Windows")
endif()

set(CV2PDB_ROOT "${CMAKE_CURRENT_SOURCE_DIR}/../3rd/cv2pdb")
set(CV2PDB_SRC "${CV2PDB_ROOT}/src")

set(SOURCES
    ${CV2PDB_SRC}/cv2pdb.cpp
    ${CV2PDB_SRC}/cvutil.cpp
    ${CV2PDB_SRC}/demangle.cpp
    ${CV2PDB_SRC}/dwarf2pdb.cpp
    ${CV2PDB_SRC}/dwarflines.cpp
    ${CV2PDB_SRC}/main.cpp
    ${CV2PDB_SRC}/mspdb.cpp
    ${CV2PDB_SRC}/PEImage.cpp
    ${CV2PDB_SRC}/readDwarf.cpp
    ${CV2PDB_SRC}/symutil.cpp
)

if(CMAKE_SIZEOF_VOID_P EQUAL 8)
    enable_language(ASM_MASM)
    list(APPEND SOURCES ${CV2PDB_SRC}/cvt80to64.asm)
endif()

add_executable(cv2pdb ${SOURCES})

target_include_directories(cv2pdb PRIVATE
    ${CV2PDB_SRC}
    ${CV2PDB_SRC}/packages/Microsoft.VisualStudio.Setup.Configuration.Native.1.16.30/lib/native/include
)

target_link_libraries(cv2pdb PRIVATE dbghelp)

target_compile_definitions(cv2pdb PRIVATE
    WIN32
    _CONSOLE
    NDEBUG
)

if(MSVC)
    target_compile_options(cv2pdb PRIVATE /wd4996 /Zc:wchar_t-)
endif()
