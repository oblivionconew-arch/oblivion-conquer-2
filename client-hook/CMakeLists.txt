cmake_minimum_required(VERSION 3.15)
project(ConquerHook LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

if(MSVC)
    # Static CRT (/MT) in Release -> zero dependency on vcruntime*.dll or Debug CRT
    set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
    add_compile_options(/W4 /O2 /DNDEBUG /D_CRT_SECURE_NO_WARNINGS /D_WINSOCK_DEPRECATED_NO_WARNINGS)
elseif(MINGW)
    add_compile_options(-Wall -Wextra -O2 -DNDEBUG)
    add_link_options(-static -static-libgcc -static-libstdc++ -s)
endif()

add_library(ConquerHook SHARED
    src/dllmain.cpp
    src/config.cpp
    src/iat_hook.cpp
)
target_link_libraries(ConquerHook PRIVATE ws2_32 shell32 user32)
set_target_properties(ConquerHook PROPERTIES PREFIX "")

add_executable(PlayLauncher WIN32
    src/launcher_main.cpp
)
target_link_libraries(PlayLauncher PRIVATE user32 kernel32)
