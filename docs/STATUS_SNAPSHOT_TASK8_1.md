# STATUS SNAPSHOT (Task 8 / Task 8.1)

## 1. Project purpose

EmbeddedCMakeGen is currently a CLI that scans an embedded C project tree (`.c/.h/.s/.S/.ld`), chooses an analyzer, builds a unified `ProjectModel`, and generates a CMake-based build bootstrap. It is designed to work from source layout rather than vendor project metadata.  

- **Near-term V1 goal (STM32):** recognize STM32CubeMX-style source trees and generate a usable STM32-oriented modular CMake skeleton.
- **Long-term goal:** keep the architecture platform-agnostic enough to support additional embedded platforms and analyzers beyond STM32.

> Note: V1 boundary docs still mention legacy generated files (`cmake/generated_sources.cmake`, `cmake/generated_platform.cmake`), but current implementation has moved to a modular STM32 module path (`cmake/stm32cubemx/CMakeLists.txt`).

---

## 2. Current architecture summary

### Scanner
- `ProjectScanner` recursively walks directories, ignoring common build/IDE folders.
- It classifies discovered files into: C sources, headers, assembly, linker scripts, and directory list.
- Scanner itself has no explicit platform-specific heuristics.

### Analyzer selector
- `AnalyzerSelector` evaluates all analyzers by confidence, then priority.
- If best confidence is below threshold (40), it prefers generic analyzer fallback if present.

### STM32 analyzer
- `Stm32ProjectAnalyzer` performs confidence matching using STM32-specific signals (CMSIS, HAL, startup/system files, STM32 headers).
- It resolves startup file, linker script, chip macro, include dirs, source grouping, compile definitions, and baseline target flags.
- It fills many STM32-oriented `ProjectModel` fields (module style/path, toolchain kind, startup/linker selection metadata).

### Generic analyzer
- `GenericEmbeddedCAnalyzer` gives a broad embedded-C fallback based mainly on file presence.
- It sets basic source/include/linker data, plus minimal options from user overrides.

### ProjectModel
- `ProjectModel` is the normalization point consumed by generation.
- It now contains both legacy aggregate fields (`SourceFiles`, `AsmFiles`) and modular grouped fields (`ApplicationSources`, `DriverSources`, `MiddlewareSources`, `StartupSources`) plus metadata (toolchain/presets/analyzer selection).

### CMake generator
- `CMakeGenerator` currently outputs five files:
  - root `CMakeLists.txt`
  - `CMakePresets.json`
  - `cmake/gcc-arm-none-eabi.cmake`
  - `cmake/starm-clang.cmake`
  - `cmake/stm32cubemx/CMakeLists.txt`
- It builds an STM32 module style by default and emits list variables + target wiring.

### Generated file writer
- `GeneratedFileWriter` supports preview-only mode, overwrite behavior, optional backup, and atomic replacement.

### Console command flow
- `Program.cs` delegates to `CommandDispatcher`.
- Flow: parse args → scan → (optionally print scan summary) → analyzer select/analyze → generate files → preview or write.
- CLI commands: `scan`, `preview`, `generate`.

---

## 3. Current generated output structure

Current generator emits this structure:

```text
<output-root>/
├─ CMakeLists.txt
├─ CMakePresets.json
└─ cmake/
   ├─ gcc-arm-none-eabi.cmake
   ├─ starm-clang.cmake
   └─ stm32cubemx/
      └─ CMakeLists.txt
```

### Notes on current behavior
- Root `CMakeLists.txt` creates executable target and adds `add_subdirectory(cmake/stm32cubemx)`.
- The STM32 module file defines source/include/flags/link variables and configures:
  - `stm32cubemx_common` (INTERFACE)
  - `stm32cubemx_drivers` (OBJECT)
  - main target source/link/include/defs/options wiring
  - optional linker script `-T...`.
- Paths in the STM32 module are rewritten as `${CMAKE_CURRENT_SOURCE_DIR}/../../<relative-path>` for non-absolute, non-generator-expression entries.

---

## 4. ProjectModel status

## Confirmed existing fields
- Core identity/platform: `ProjectName`, `TargetName`, `PlatformKind`.
- Source structure: `SourceFiles`, `AsmFiles`, `ApplicationSources`, `DriverSources`, `MiddlewareSources`, `StartupSources`.
- Build config: `IncludeDirectories`, `LinkDirectories`, `LinkerScript`, `CompileDefinitions`, `CompileOptions`, `LinkOptions`, `LinkedLibraries`.
- Toolchain/presets: `ToolchainFile`, `ToolchainKind`, `SupportedBuildTypes`, `PresetGenerator`.
- STM32/platform metadata: `ChipMacro`, `UseHalDriver`, `PlatformFamily`, `PlatformSeries`.
- Generation metadata: `CMakeModuleStyle`, `PlatformModuleRelativePath`, `SelectedAnalyzerName`, `SelectedStartupFile`, `SelectedLinkerScript`.

## Partially integrated fields
- `SourceFiles` / `AsmFiles` are still populated but current module generation primarily consumes grouped source fields.
- `PlatformModuleRelativePath` and `CMakeModuleStyle` exist in the model but generator currently uses hardcoded output path constants.
- `ToolchainKind` is stored but not used to conditionally choose which toolchain file(s) are emitted.

## Planned-looking or not fully wired through generation
- From `UserProjectOptions`: `IncludeCommonStm32Definitions` and `CStandard` are present but not applied in current generator/analyzer output logic.
- Some override options exist in model/options domain but are not exposed through CLI parsing (e.g., many list overrides, build types, preset generator, toolchain kind/path).

---

## 5. STM32 analysis status

STM32 recognition is currently heuristic-based and confidence-scored.

### Implemented heuristics/signals
- `Drivers/CMSIS` path detection.
- STM32 HAL driver path detection (`drivers/stm32...hal_driver` pattern).
- `startup_stm32*.s` / `.S` detection.
- `system_stm32*.c` detection.
- `stm32*.h` header detection.
- `stm32*_hal_conf.h` detection.

### Additional STM32-specific analysis behavior
- Linker script preference for `STM32*_FLASH.ld`, fallback to first `.ld`.
- Chip macro inference from startup/header/linker file name tokens matching `STM32[A-Z0-9]+`.
- Family/series derivation from inferred chip macro.
- Baseline MCU flags currently inferred as:
  - always: `-mcpu=cortex-m4`, `-mthumb`
  - adds FPU flags only for `STM32G4*` chip macros.

---

## 6. Current known real-project validation results

Based strictly on current repository content (code + docs), the following can be stated:

## Confirmed
- Preview mode exists and logs per-file create/overwrite actions without writing.
- Modular STM32 output files are generated by design (`cmake/stm32cubemx/CMakeLists.txt` plus toolchains/presets).

## Not confirmable from repository state alone
- No committed real STM32 project fixture or validation report is present in this repository.
- No committed configure/build transcripts for a real STM32CubeMX project were found.

## Current status statement
- **Preview generation path is implemented and inspectable in code.**
- **Configure/build success against a real STM32 project cannot be claimed from available repository evidence.**

---

## 7. Current known build/configuration problems

> This section is an implementation-based risk/problem inventory from current code; where runtime proof is absent, that is explicitly noted.

### 7.1 Potential source path base mismatch in `cmake/stm32cubemx/CMakeLists.txt`
- **Symptom:** Generated file paths are prefixed with `${CMAKE_CURRENT_SOURCE_DIR}/../../`.
- **Probable root cause:** `ToPlatformModulePath` assumes module file location under `cmake/stm32cubemx` and hardcodes `../../` rebasing.
- **Likely location:** `CMakeGenerator.ToPlatformModulePath` and `BuildPlatformModule`.
- **Risk:** If generation/output layout changes (or if module path becomes configurable), path resolution may break.

### 7.2 Partial `ProjectModel` → generator wiring
- **Symptom:** Model includes `PlatformModuleRelativePath`, `CMakeModuleStyle`, `ToolchainKind` but generator uses fixed constants.
- **Probable root cause:** Generator path/toolchain selection not yet parameterized from model metadata.
- **Likely location:** `CMakeGenerator` constants and `Generate(...)` file list construction.

### 7.3 Cortex-M flags are currently simplistic
- **Symptom:** Default compile/link MCU flags are mostly fixed to Cortex-M4 (`-mcpu=cortex-m4 -mthumb`) with only a small G4 branch.
- **Probable root cause:** Inference logic is intentionally minimal and not yet family-accurate across STM32 lines.
- **Likely location:** `Stm32ProjectAnalyzer.ResolveMcuFlags`.
- **Risk:** Wrong ABI/FPU/CPU flags for non-G4 families can lead to compile/link/runtime issues.

### 7.4 Toolchain file may be insufficient for practical STM32 builds
- **Symptom:** Generated GCC toolchain file sets compiler/bin tools and find-root modes but omits common embedded defaults (flags/specs/sysroot handling, objdump/size post-build hooks, etc.).
- **Probable root cause:** Toolchain template currently minimal bootstrap.
- **Likely location:** `CMakeGenerator.BuildGccToolchainFile`.

### 7.5 CLI exposes only a subset of available option overrides
- **Symptom:** `UserProjectOptions` supports many overrides, but `CommandDispatcher` maps only a small set from CLI.
- **Probable root cause:** CLI argument parser not expanded alongside model growth.
- **Likely location:** `CommandDispatcher.ParsedCommand` and `BuildUserOptions`.

### 7.6 Compile definitions/include propagation concern (status)
- **Symptom category requested:** definitions/includes may not effectively apply everywhere.
- **Current code state:** both `stm32cubemx_common` (INTERFACE) and direct target settings on driver/main targets are present; this suggests explicit propagation attempts are implemented.
- **Uncertainty:** No real-project compile transcript in repo to confirm whether propagation is fully correct in all compilation units.

---

## 8. Toolchain file status

Current generated `cmake/gcc-arm-none-eabi.cmake` contains:

- `CMAKE_SYSTEM_NAME Generic`, `CMAKE_SYSTEM_PROCESSOR arm`.
- `CMAKE_TRY_COMPILE_TARGET_TYPE STATIC_LIBRARY`.
- Toolchain prefix resolution with optional `TOOLCHAIN_BIN_PATH` cache path.
- Assignments for `CMAKE_C_COMPILER`, `CMAKE_ASM_COMPILER`, `CMAKE_CXX_COMPILER`, `CMAKE_LINKER`, `CMAKE_AR`, `CMAKE_OBJCOPY`, `CMAKE_SIZE`.
- `CMAKE_FIND_ROOT_PATH_MODE_*` settings (program NEVER, others ONLY).

### Clearly missing vs practical STM32 GCC toolchain setups
- No default language standard/cross compile common flags.
- No debug/release flag strategy.
- No explicit `--specs` usage (`nano.specs`, `nosys.specs`) or libc strategy.
- No section-gc defaults (`-ffunction-sections`, `-fdata-sections`, linker gc).
- No per-family CPU/FPU/ABI mapping at toolchain level.
- No binutils extras (`objdump`, `nm`, `ranlib`, `strip`) wiring.
- No post-build size/map/hex/bin conversion helpers.

---

## 9. Current gap versus STM32CubeMX-style output

Major differences relative to typical CubeMX-generated CMake projects:

- Current generator is **scan/inference-driven**, not `.ioc`-driven.
- CPU/floating-point flag configuration is not as complete/family-specific as typical CubeMX exports.
- Toolchain and linker defaults are leaner and less STM32-opinionated.
- Output uses custom module layering (`stm32cubemx_common`, `stm32cubemx_drivers`) rather than CubeMX’s exact template structure.
- Real-project parity (configure+build success) is not yet documented in-repo.

---

## 10. Multi-platform extensibility status

### Positive signs
- Scanner remains platform-agnostic.
- Analyzer strategy pattern exists (`IProjectAnalyzer` + selector + generic fallback).
- Generation depends on unified `ProjectModel` rather than analyzer internals.

### Over-coupling risks introduced by STM32-focused evolution
- `CMakeGenerator` currently hardcodes STM32-oriented output paths/names (`cmake/stm32cubemx`, `stm32cubemx_*` targets).
- Root `CMakeLists.txt` unconditionally adds STM32 module subdirectory.
- Model has platform/module metadata, but generator does not yet use it for platform-specific branching.

Overall: extensibility foundation exists, but generator implementation is presently STM32-coupled in concrete output wiring.

---

## 11. Recommended next fix order

1. **Parameterize generator by `ProjectModel` platform/module metadata** instead of hardcoded STM32 constants.
2. **Harden path rebasing logic** for platform module sources/includes/linker script to avoid layout-sensitive `../../` assumptions.
3. **Expand STM32 MCU flag mapping** by inferred family/series and ensure compile/link consistency.
4. **Strengthen GCC toolchain template** with practical embedded defaults and configuration hooks.
5. **Add repository-tracked real-project validation evidence** (configure/build logs or scripted checks) for STM32CubeMX-style projects.
6. **Expose additional CLI overrides** already present in `UserProjectOptions` so behavior can be validated without code edits.

---

## 12. Repository file references

Most relevant files for understanding current state:

- `EmbeddedCMakeGen/Program.cs`
- `EmbeddedCMakeGen/Commands/CommandDispatcher.cs`
- `EmbeddedCMakeGen/Domain/Models/ProjectModel.cs`
- `EmbeddedCMakeGen/Domain/Models/UserProjectOptions.cs`
- `EmbeddedCMakeGen/Infrastructure/Scanning/ProjectScanner.cs`
- `EmbeddedCMakeGen/Infrastructure/Scanning/ScannerDefaults.cs`
- `EmbeddedCMakeGen/Infrastructure/Analysis/AnalyzerSelector.cs`
- `EmbeddedCMakeGen/Infrastructure/Analysis/Stm32ProjectAnalyzer.cs`
- `EmbeddedCMakeGen/Infrastructure/Analysis/GenericEmbeddedCAnalyzer.cs`
- `EmbeddedCMakeGen/Infrastructure/Generation/CMakeGenerator.cs`
- `EmbeddedCMakeGen/Infrastructure/IO/GeneratedFileWriter.cs`
- `docs/V1_Boundary.md`
- `README.md`

