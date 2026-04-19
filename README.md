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

## Examples

### 1) Scan only

```bash
dotnet run --project EmbeddedCMakeGen -- scan --root ./MyFirmware
```

### 2) Preview generated results (dry run)

```bash
dotnet run --project EmbeddedCMakeGen -- preview --root ./MyFirmware --platform stm32
```

### 3) Generate files into project root

```bash
dotnet run --project EmbeddedCMakeGen -- generate --root ./MyFirmware --backup
```

### 4) Generate files into a separate output folder

```bash
dotnet run --project EmbeddedCMakeGen -- generate --root ./MyFirmware --out ./build-config
```

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

## 使用方式

```bash
dotnet run --project EmbeddedCMakeGen -- <scan|preview|generate> --root <projectRoot> [options]
```

### 命令说明

- `scan`：扫描工程并输出统计摘要。
- `preview`：分析并预览生成结果（不写入文件）。
- `generate`：分析并将生成结果写入输出目录。

### 参数说明

- `--root <projectRoot>`：**必填**，嵌入式工程根目录。
- `--out <outputDir>` / `--output <outputDir>`：生成文件输出目录（默认使用 `--root`）。
- `--project-name <name>`：覆盖生成内容中的项目名。
- `--target-name <name>`：覆盖 CMake 目标名。
- `--platform <stm32|generic>`：强制指定分析平台。
- `--chip <chipId>`：覆盖芯片宏/芯片 ID（用于模板生成）。
- `--startup <path>`：指定启动文件路径。
- `--linker <path>`：指定链接脚本路径。
- `--backup`：覆盖已有文件前先创建备份。

## 示例

### 1）仅扫描

```bash
dotnet run --project EmbeddedCMakeGen -- scan --root ./MyFirmware
```

### 2）预览生成结果（不落盘）

```bash
dotnet run --project EmbeddedCMakeGen -- preview --root ./MyFirmware --platform stm32
```

### 3）生成到工程根目录

```bash
dotnet run --project EmbeddedCMakeGen -- generate --root ./MyFirmware --backup
```

### 4）生成到独立输出目录

```bash
dotnet run --project EmbeddedCMakeGen -- generate --root ./MyFirmware --out ./build-config
```
