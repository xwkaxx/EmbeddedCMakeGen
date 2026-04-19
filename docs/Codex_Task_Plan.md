# Codex Task Plan

## Development Strategy

Use Codex incrementally.
Do not ask Codex to generate the whole project in one step.

Each task must:
- stay within V1 scope
- keep the project compiling
- avoid introducing unnecessary abstractions
- avoid mixing platform logic into scanner or generator

## Task 1 - Project Skeleton

Create the initial folder structure and minimal compilable classes.

Target:
- Commands
- Application
- Domain
- Infrastructure
- Templates

Create minimal files for:
- Program.cs
- CommandDispatcher.cs
- ScanResult.cs
- ProjectModel.cs
- IProjectScanner.cs
- IProjectAnalyzer.cs
- ICMakeGenerator.cs
- ProjectScanner.cs
- Stm32ProjectAnalyzer.cs
- GenericEmbeddedCAnalyzer.cs
- CMakeGenerator.cs
- GeneratedFileWriter.cs
- PathNormalizer.cs
- ConsoleLogger.cs

## Task 2 - Domain Models and Interfaces

Implement the core models and interfaces only.
No real scanner logic yet.

## Task 3 - Project Scanner

Implement recursive scanning for:
- *.c
- *.h
- *.s
- *.S
- *.ld

Ignore common build and IDE folders.

## Task 4 - Analyzer Selection and Platform Analysis

Implement:
- AnalyzerSelector
- Stm32ProjectAnalyzer
- GenericEmbeddedCAnalyzer

## Task 5 - CMake File Generation

Implement generation of:
- CMakeLists.txt
- CMakePresets.json
- cmake/generated_sources.cmake
- cmake/generated_platform.cmake

## Task 6 - Preview and File Writing

Implement:
- preview mode
- overwrite control
- backup before overwrite
- atomic file replacement

## Important Constraints

- Scanner must remain platform-agnostic
- Analyzer must own platform recognition
- Generator must depend only on ProjectModel
- User options must override auto-detected values
- No GUI
- No C++
- No multi-target support
