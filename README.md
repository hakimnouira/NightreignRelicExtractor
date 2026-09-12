# Nightreign Relic Extractor

[![Platform](https://img.shields.io/badge/Platform-Windows-0078D6?logo=windows&logoColor=white)](#)
[![Runtime](https://img.shields.io/badge/.NET_Framework-4.8-512BD4?logo=dotnet&logoColor=white)](#)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](#)
[![Mode](https://img.shields.io/badge/Mode-Strict_Read--Only-brightgreen)](#)

A fast, lightweight, and completely standalone Windows tool whose sole purpose is to extract your relic inventory from an **Elden Ring Nightreign** `.co2` or `.sl2` save file directly into **CSV** and **Excel (.xlsx)**.

---

![Nightreign Relic Extractor Screenshot](assets/screenshot.png)

---

## Highlights

- **Works on Any Windows PC**: Portable, native standalone application (`NightreignRelicExtractor.exe`). No Python, no Node.js, and no external runtime installations required. Works immediately on Windows 10 and 11.
- **Strict Read-Only Guarantee**: Opens save files strictly with read-only access flags (`FileAccess.Read`, `FileShare.ReadWrite`). **Never modifies, overwrites, or alters your original save file**.
- **Extracts Every Relic Instance**: Every physical relic instance in your save file becomes its own row. Duplicate relics (e.g. multiple *Grand Tranquil Scene* relics with different effects) are never merged.
- **Accurate Unknown Effect Handling**: If an effect ID is not yet documented in community data dictionaries, the tool will **not** guess or skip the relic. It marks the effect name as `UNKNOWN` and preserves the exact original **Effect ID** and **Raw Value**.
- **Dual Interface**:
  - **GUI Mode**: Clean Elden Ring-themed interface with **Browse**, **Auto-Detect** (scans `%APPDATA%\Nightreign\`), drag-and-drop support, and one-click buttons to open CSV, Excel, or the destination folder.
  - **CLI / Drag-to-EXE Mode**: Run from Command Prompt/PowerShell or drag your `.co2` file directly onto the executable icon.

---

## How to Use

### Method 1: Graphical Interface (Recommended)
1. Double-click **`NightreignRelicExtractor.exe`**.
2. Click **Auto-Detect** to find your save automatically in AppData, click **Browse...** to choose it manually, or simply drag and drop `NR0000.co2` anywhere onto the window.
3. Click **Extract Relics**.
4. Use the quick buttons (**Open CSV**, **Open Excel**, **Open Output Folder**) to immediately view your inventory!

### Method 2: Drag and Drop onto Executable
- Drag your `NR0000.co2` file directly onto the **`NightreignRelicExtractor.exe`** file icon in Windows Explorer.
- The `relics.csv` and `relics.xlsx` files will be generated right next to your save file.

### Method 3: Command Line
Open PowerShell or Command Prompt in the folder:
```powershell
.\NightreignRelicExtractor.exe NR0000.co2
```

Console Output:
```text
Nightreign Relic Extractor
Input: NR0000.co2
Relics found: 118
CSV: relics.csv
Excel: relics.xlsx
Original save modified: NO
```

---

## Output Files

The tool generates two files in the same folder as your input save:

1. **`relics.csv`**:
   - Encoded in **UTF-8 with BOM** for perfect display in Microsoft Excel, LibreOffice, and Google Sheets without encoding issues.
2. **`relics.xlsx`**:
   - Ready-to-use Excel workbook with:
     - Styled headers with contrasting dark slate fill and bold white text.
     - **Frozen top row** so headers remain visible when scrolling.
     - **Auto-Filter enabled** on all 18 columns for sorting and filtering by Color, Type, Name, or specific Effects.
     - Auto-sized columns formatted for high readability.

### Columns Included

| # | Column Name | Description |
|---|---|---|
| 1 | `#` | Row index (1-indexed) |
| 2 | `Relic ID` | Unique instance ID of the physical relic in the save |
| 3 | `Item ID` | Base item ID (maps to the relic model/name) |
| 4 | `Relic Name` | Relic name (e.g. *Grand Tranquil Scene*, *Night Of The Baron*) |
| 5 | `Color` | Relic color (*Red*, *Blue*, *Yellow*, *Green*) |
| 6 | `Relic Type` | Relic classification (*Relic*, *DeepRelic*, *UniqueRelic*) |
| 7–9 | `Effect 1 ID` / `Effect 1` / `Effect 1 Value` | Primary effect ID, name, and magnitude/value |
| 10–12 | `Effect 2 ID` / `Effect 2` / `Effect 2 Value` | Secondary effect ID, name, and magnitude/value |
| 13–15 | `Effect 3 ID` / `Effect 3` / `Effect 3 Value` | Tertiary effect ID, name, and magnitude/value |
| 16–18 | `Effect 4 ID` / `Effect 4` / `Effect 4 Value` | Quaternary effect (empty if relic has fewer than 4 effects) |

---

## Building from Source

The project uses only native .NET Framework APIs pre-installed on Windows 10 and 11. No external packages, NuGet restores, or compiler installations are required.

To rebuild `NightreignRelicExtractor.exe`:
- Double-click **`build.bat`**, or
- Run the following command from PowerShell:
```powershell
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /codepage:65001 /utf8output /optimize+ /target:exe /out:NightreignRelicExtractor.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:System.Web.Extensions.dll /res:items_data.json,items_data.json /res:effects_data.json,effects_data.json NightreignRelicExtractor.cs
```

---

## Acknowledgments & Research

Relic container structures and item/effect databases are based on open-source research from [ERN_RelicForge](https://github.com/cetusk/ERN_RelicForge).

## License

This project is licensed under the [MIT License](LICENSE).
