# CMake Environment Generator V1 Boundary

## Project Goal

EmbeddedCMakeGen is a C# console application that scans an embedded C project directory
and generates the basic CMake environment files required to build the project.

## Long-Term Goal

The long-term goal is to support multi-platform embedded C projects without relying on
vendor-specific project files such as CubeMX, Keil, IAR, or Visual Studio project metadata.

The generator should primarily work from scanned files such as:

- *.c
- *.h
- *.s / *.S
- *.ld

## V1 Scope

V1 focuses on STM32-style embedded C projects, while the architecture must remain extensible
for future multi-platform support.

## V1 Must Support

- C# console application
- Recursive scan of a specified root directory
- Detection of:
  - C source files
  - header files
  - assembly startup files
  - linker scripts
- Basic platform analysis
- STM32-style project recognition
- Fallback generic embedded C project analysis
- Generation of:
  - CMakeLists.txt
  - CMakePresets.json
  - cmake/generated_sources.cmake
  - cmake/generated_platform.cmake

## V1 Must Not Support

- C++
- multi-target projects
- GUI
- CubeMX .ioc parsing
- Keil/IAR/VS project parsing
- automatic generation of startup files
- automatic generation of linker scripts
- complex rule DSL systems

## Architecture Constraints

- Scanner must not contain platform-specific logic
- Platform-specific analysis must be isolated in analyzers
- CMake generation must depend only on a unified ProjectModel
- Generated content must be separated from handwritten content
- User options must override auto-detected results

## Safety Constraints

- Preview mode must not write files
- Generate mode may overwrite only generated files
- Backup must be supported before overwrite
- Paths must be normalized to relative paths with forward slashes
