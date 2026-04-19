# EmbeddedCMakeGen

English | [简体中文](#简体中文)

A lightweight CLI tool that scans embedded C projects and generates CMake build files.

## Features

- Scan project source layout (`.c`, `.h`, `.s/.S`, `.ld`).
- Auto-select analyzer (STM32 or generic embedded C).
- Generate `CMakeLists.txt` and related build files.
- Support `scan`, `preview`, and `generate` workflows.
- Optional backup before overwriting generated files.

## Requirements

- .NET SDK 10.0 (target framework: `net10.0`)

## Build

```bash
dotnet build EmbeddedCMakeGen/EmbeddedCMakeGen.csproj
```

## Quick Start

```bash
# 1) Scan project
dotnet run --project EmbeddedCMakeGen -- scan --root ./MyFirmware

# 2) Preview generation result without writing files
dotnet run --project EmbeddedCMakeGen -- preview --root ./MyFirmware

# 3) Actually generate CMake files
dotnet run --project EmbeddedCMakeGen -- generate --root ./MyFirmware --backup
```

## Usage

```bash
dotnet run --project EmbeddedCMakeGen -- <scan|preview|generate> --root <projectRoot> [options]
```

### Commands

- `scan`: scan project files and print summary.
- `preview`: analyze and preview generated files (no file write).
- `generate`: analyze and write generated files to output directory.

### Options

- `--root <projectRoot>`: **required**, root directory of your embedded project.
- `--out <outputDir>` / `--output <outputDir>`: output directory for generated files (default: `--root`).
- `--project-name <name>`: override project name used in generated content.
- `--target-name <name>`: override CMake target name.
- `--platform <stm32|generic>`: force analyzer platform.
- `--chip <chipId>`: override chip macro/ID (for template generation).
- `--startup <path>`: specify startup file path.
- `--linker <path>`: specify linker script path.
- `--backup`: create a backup before overwriting existing files.

### Generated files

By default, `generate` writes files into `--root` (or `--out` if specified):

- `CMakeLists.txt`
- `CMakePresets.json`
- `cmake/gcc-arm-none-eabi.cmake`
- `cmake/starm-clang.cmake`
- `cmake/stm32cubemx/CMakeLists.txt`

## Detailed examples

### 1) Scan only

```bash
dotnet run --project EmbeddedCMakeGen -- scan --root ./MyFirmware
```

Use when you only want to inspect source layout and file statistics.

### 2) Preview generated results (dry run)

```bash
dotnet run --project EmbeddedCMakeGen -- preview --root ./MyFirmware --platform stm32
```

Use when you need to confirm analyzer selection and would-be write actions before touching files.

### 3) Generate files into project root with backup

```bash
dotnet run --project EmbeddedCMakeGen -- generate --root ./MyFirmware --backup
```

Use when files already exist and you want to keep backup snapshots before overwrite.

### 4) Generate files into a separate output folder

```bash
dotnet run --project EmbeddedCMakeGen -- generate --root ./MyFirmware --out ./build-config
```

Use when you want to keep source tree clean or compare generated outputs.

### 5) Override project and target naming

```bash
dotnet run --project EmbeddedCMakeGen -- generate \
  --root ./MyFirmware \
  --project-name MyBoardFirmware \
  --target-name my_board_app
```

Use when existing repo naming does not match desired CMake project/target names.

### 6) Force generic analyzer and explicit startup/linker

```bash
dotnet run --project EmbeddedCMakeGen -- generate \
  --root ./MyFirmware \
  --platform generic \
  --startup ./startup/startup_stm32f407xx.s \
  --linker ./linker/STM32F407VGTx_FLASH.ld
```

Use when auto-detection is not ideal or project structure is non-standard.

---

## 简体中文

一个轻量级命令行工具，用于扫描嵌入式 C 工程并生成 CMake 构建文件。

## 功能特性

- 扫描工程源码结构（`.c`、`.h`、`.s/.S`、`.ld`）。
- 自动选择分析器（STM32 或通用嵌入式 C）。
- 生成 `CMakeLists.txt` 及相关构建文件。
- 支持 `scan`、`preview`、`generate` 三种流程。
- 支持覆盖写入前自动备份。

## 环境要求

- .NET SDK 10.0（目标框架：`net10.0`）

## 构建

```bash
dotnet build EmbeddedCMakeGen/EmbeddedCMakeGen.csproj
```

## 快速上手

```bash
# 1）扫描工程
dotnet run --project EmbeddedCMakeGen -- scan --root ./MyFirmware

# 2）预览生成结果（不写入文件）
dotnet run --project EmbeddedCMakeGen -- preview --root ./MyFirmware

# 3）正式生成，并在覆盖前备份
dotnet run --project EmbeddedCMakeGen -- generate --root ./MyFirmware --backup
```

## 使用方式

```bash
dotnet run --project EmbeddedCMakeGen -- <scan|preview|generate> --root <projectRoot> [options]
```

### 命令说明

- `scan`：扫描工程并输出统计摘要。
- `preview`：分析并预览生成结果（不写入文件）。
- `generate`：分析并将生成结果写入输出目录。

### 参数说明（详细）

- `--root <projectRoot>`：**必填**，嵌入式工程根目录。
- `--out <outputDir>` / `--output <outputDir>`：生成文件输出目录（默认使用 `--root`）。
- `--project-name <name>`：覆盖生成内容中的项目名（`project(...)` 名称）。
- `--target-name <name>`：覆盖 CMake 目标名（`add_executable(...)` 名称）。
- `--platform <stm32|generic>`：强制指定分析平台；不指定时自动选择。
- `--chip <chipId>`：覆盖芯片宏/芯片 ID（用于模板与编译定义生成）。
- `--startup <path>`：指定启动文件路径（如 `.s/.S`）。
- `--linker <path>`：指定链接脚本路径（如 `.ld`）。
- `--backup`：覆盖已有文件前先创建备份。

### 生成文件列表

默认会生成到 `--root`（或 `--out` 指定目录）下的以下文件：

- `CMakeLists.txt`
- `CMakePresets.json`
- `cmake/gcc-arm-none-eabi.cmake`
- `cmake/starm-clang.cmake`
- `cmake/stm32cubemx/CMakeLists.txt`

## 详细使用说明（按场景）

### 场景 1：只想了解工程扫描结果

```bash
dotnet run --project EmbeddedCMakeGen -- scan --root ./MyFirmware
```

适合在接手一个新工程时快速确认源码、头文件、汇编文件、链接脚本数量。

### 场景 2：先看会生成什么，再决定是否落盘

```bash
dotnet run --project EmbeddedCMakeGen -- preview --root ./MyFirmware --platform stm32
```

适合先验证分析器选择、启动文件/链接脚本识别结果，避免直接改动工程。

### 场景 3：直接生成到工程目录，并自动备份原文件

```bash
dotnet run --project EmbeddedCMakeGen -- generate --root ./MyFirmware --backup
```

适合已有旧版 CMake 文件，需要平滑覆盖并保留回滚能力。

### 场景 4：生成到独立目录，便于对比

```bash
dotnet run --project EmbeddedCMakeGen -- generate --root ./MyFirmware --out ./build-config
```

适合将生成结果与仓库主目录解耦，或用于 CI 中对比输出差异。

### 场景 5：指定项目名和目标名

```bash
dotnet run --project EmbeddedCMakeGen -- generate \
  --root ./MyFirmware \
  --project-name MyBoardFirmware \
  --target-name my_board_app
```

适合仓库目录名与最终交付物命名不一致的情况。

### 场景 6：强制通用平台并手动指定启动文件/链接脚本

```bash
dotnet run --project EmbeddedCMakeGen -- generate \
  --root ./MyFirmware \
  --platform generic \
  --startup ./startup/startup_stm32f407xx.s \
  --linker ./linker/STM32F407VGTx_FLASH.ld
```

适合工程目录结构特殊、自动识别不稳定或需要精确控制输入文件时。

## 常见问题

### 1）提示 `Missing required argument: --root`
请确认命令中包含 `--root <你的工程路径>`。

### 2）提示 `Root directory does not exist`
请检查 `--root` 路径是否正确，建议使用绝对路径排查问题。

### 3）想看帮助信息
可直接运行一个错误命令（如不带参数运行），程序会输出完整 usage。
