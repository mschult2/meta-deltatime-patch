cmake_minimum_required(VERSION 3.10)
project(openxr_frame_stats C)

add_library(openxr_frame_stats SHARED openxr_frame_stats.c)

target_include_directories(openxr_frame_stats PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/openxr/include
)
