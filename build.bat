@echo off
setlocal
echo ===================================================
echo Building Nightreign Relic Extractor...
echo ===================================================

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

if not exist "%CSC%" (
    echo Error: .NET Framework compiler not found at %CSC%
    pause
    exit /b 1
)

"%CSC%" /nologo /codepage:65001 /utf8output /optimize+ /target:exe /out:NightreignRelicExtractor.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:System.Web.Extensions.dll /res:items_data.json,items_data.json /res:effects_data.json,effects_data.json Localization.cs AIPromptBuilder.cs PresetBrowserDialog.cs PresetManager.cs SaveRelicWriter.cs OverlayForm.cs RelicPickerDialog.cs NightreignRelicExtractor.cs

if %errorlevel% equ 0 (
    echo.
    echo Build SUCCESSFUL! Created NightreignRelicExtractor.exe
    echo.
) else (
    echo.
    echo Build FAILED with error code %errorlevel%
    echo.
)

pause
