# Nightreign Relic Extractor & Vessel Loadout Manager
## Master Project Context & Onboarding Guide for Antigravity IDE

> **Previous Chat Reference in Antigravity**:
> Conversation ID: `68d99089-024a-4c12-b8a6-7aa5f277e899`
> Clickable link inside Antigravity: [Previous Conversation](conversation://68d99089-024a-4c12-b8a6-7aa5f277e899)

---

### 1. Project Overview
**Nightreign Relic Extractor** is a standalone Windows desktop tool for *Elden Ring: Nightreign* (`NR0000.co2` save files). It allows players to:
1. Decrypt, read, inspect, and extract all collected relics from their save file.
2. View and build character vessel loadouts (Urn, Goblet, Chalice, etc.) for all 10 characters with color affinity slot matching (Red, Blue, Yellow, Green, Any) and full in-game effect descriptions.
3. Automatically equip builds directly into the decrypted save file with automatic MD5 checksum recalculation and instant in-game updates.
4. Browse and snapshot presets with detailed relic effect cards.
5. Generate rich AI prompts (for ChatGPT/Claude) to design Meta, Fun, Support, and Hybrid builds, and import them seamlessly via JSON.
6. Toggle an in-game transparent HUD overlay (Global Hotkey **F10**) while playing.
7. Safeguard save files with an automatic 4-backup timestamped rotation system.
8. Full multi-language support: English, French, and Arabic (with RTL formatting).

---

### 2. Architecture & Tech Stack
* **Language & Framework**: Pure C# targeting **.NET Framework 4.8** (compatible with native Windows installations without requiring external runtimes).
* **Compiler**: `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` (C# 5 syntax; **no** C# 7+ features like `out var` or pattern matching).
* **Dependencies**: **Zero external NuGet dependencies** (uses Windows Forms, `System.Web.Extensions.dll` for `JavaScriptSerializer`, `System.Security.Cryptography` for AES/MD5, and `System.IO.Compression`).
* **Source Files**:
  * `NightreignRelicExtractor.cs`: Main Windows Forms GUI, save file detector, tabs, slot cards, and relic table.
  * `SaveRelicWriter.cs`: Binary save decryption/encryption, dynamic marker locator, vessel table resolver, relic slot injector, MD5 checksum recalculator, and 4-backup rotator.
  * `PresetManager.cs`: JSON preset persistence (`presets.json`).
  * `PresetBrowserDialog.cs`: Visual preset browser displaying full relic cards with individual effects, character filters, and direct save injection.
  * `AIPromptBuilder.cs`: Formats current relic inventory into structured prompts for ChatGPT/Claude, requesting valid JSON builds.
  * `OverlayForm.cs`: Borderless transparent DirectX/GDI in-game overlay toggled via F10.
  * `Localization.cs`: English, French, and Arabic translations.
  * `RelicPickerDialog.cs`: Relic selector modal with filtering by character affinity and color.
  * Embedded resources: `items_data.json`, `effects_data.json`.

---

### 3. Save File Binary Specifications (`NR0000.co2`)
* **Container**: BND4 archive. Header at `0x00`, Entry 0 descriptor at `0x40` (offset 64).
* **AES-128-CBC Decryption**:
  * Key: `0x18, 0xf6, 0x32, 0x66, 0x05, 0xbd, 0x17, 0x8a, 0x55, 0x24, 0x52, 0x3a, 0xc0, 0xa0, 0xc6, 0x09`
  * IV: First 16 bytes of Entry 0 payload.
  * Decrypted buffer: First 4 bytes are length prefix; payload (`cleanData`) starts at offset 4.
* **MD5 Checksum Security**:
  * The game validates an MD5 checksum computed over the 1,048,576 bytes (`dec[4 .. 0x100004]`).
  * The resulting 16-byte MD5 hash is stored at `dec[0x100004 .. 0x100014]`. Any modification without updating this hash causes the game to report a corrupted save!
* **Dynamic Character Markers (`FindCharacterMarkersBase`)**:
  * The save file shifts based on player progression/unlocks (e.g. `0x1B91C` on early saves, `0x1C824` on advanced saves).
  * Characters are located via dynamic fingerprint: `0x0000FF01` (Wylder) followed at offset `+0x78` (120 bytes) by `0x0000FF02` (Guardian).
  * All 10 characters are spaced in 120-byte strides (`0x0000FF01 + charIndex`).
  * At `markerPos + 4`: `activeVesselId` (e.g., 1000 = Wylder Urn, 2001 = Guardian Goblet).
* **Vessel Table Layout (`FindVesselRelicOffset`)**:
  * Starts immediately following all 10 character blocks: `vesselTableBase = charMarkersBase + 10 * 0x78` (`charMarkersBase + 0x4B0`).
  * Total 70 entries (10 characters × 7 vessels: Urn, Goblet, Chalice, Soot-Covered Urn, Sealed Urn, Decrepit Goblet, Forgotten Goblet).
  * Entry stride is **28 bytes** (`0x1C`):
    * `+0x00`: `0x00000000`
    * `+0x04`: `0x00000000`
    * `+0x08`: `vesselId` (e.g., 1000, 1001, ..., 10006)
    * `+0x0C`: `relicSlot1` (uint32 Relic ID or 0)
    * `+0x10`: `relicSlot2`
    * `+0x14`: `relicSlot3`
    * `+0x18`: `0x00000000` (padding)
  * Direct formula: `entryOffset = vesselTableBase + (charIndex * 7 + vesselTypeIndex) * 28`

---

### 4. Build & Deployment
* **Build script**: `build.bat`
* **Direct compile command**:
  ```cmd
  C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /codepage:65001 /utf8output /optimize+ /target:winexe /out:NightreignRelicExtractor.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:System.Web.Extensions.dll /res:items_data.json,items_data.json /res:effects_data.json,effects_data.json Localization.cs AIPromptBuilder.cs PresetBrowserDialog.cs PresetManager.cs SaveRelicWriter.cs OverlayForm.cs RelicPickerDialog.cs NightreignRelicExtractor.cs
  ```
* **CI/CD**: GitHub Actions workflow `.github/workflows/release.yml` automatically compiles on version tags (`v*.*.*`) and attaches `NightreignRelicExtractor.exe` as a GitHub Release asset.

---

### 5. Critical Development Guidelines
1. **Never use hardcoded offsets**: Always use `FindCharacterMarkersBase(cleanData)` to resolve the base offset dynamically.
2. **Never search for small integers naively**: Searching for `1000` or `2001` before `vesselTableBase` will hit the character block's `activeVesselId` instead of the vessel table! Always constrain vessel table lookups to `charMarkersBase + 10 * 0x78` and beyond.
3. **Always preserve the 4-backup rotation**: Use `SaveRelicWriter.CreateBackupAndRotate(saveFilePath, 4)` before modifying any save.
4. **Always recalculate MD5 on save**: Recompute hash over `dec[4 .. 0x100004]` and place at `dec[0x100004]`.
5. **C# 5 Language Constraint**: When editing C# code, do NOT use C# 6/7/8+ features (`out var`, string interpolation `$"..."` if targeting older compilers, null-propagating `?.`, etc.) to keep compilation 100% compatible with native .NET 4.8 `csc.exe`.
