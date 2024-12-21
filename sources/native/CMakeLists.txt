# CMakeList.txt: 顶层 CMake 项目文件，在此处执行全局配置
# 并包含子项目。
#
cmake_minimum_required (VERSION 3.8)

# 如果支持，请为 MSVC 编译器启用热重载。
if (POLICY CMP0141)
  cmake_policy(SET CMP0141 NEW)
  set(CMAKE_MSVC_DEBUG_INFORMATION_FORMAT "$<IF:$<AND:$<C_COMPILER_ID:MSVC>,$<CXX_COMPILER_ID:MSVC>>,$<$<CONFIG:Debug,RelWithDebInfo>:EditAndContinue>,$<$<CONFIG:Debug,RelWithDebInfo>:ProgramDatabase>>")
endif()

project ("native")

#SET(CMAKE_ASM_NASM_SOURCE_FILE_EXTENSIONS asm)  # 设置NASM的文件扩展名为asm
#ENABLE_LANGUAGE(ASM_NASM)  # 让CMacke启用NASM的汇编
#SET(CMAKE_ASM_NASM_FLAGS "-g") # 让NASM生成调试信息

# 包含子项目。
add_subdirectory ("modcorenative")
