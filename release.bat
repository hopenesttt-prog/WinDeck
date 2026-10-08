@echo off
rem Double-click to build release\WinDeck-Setup-x.y.z.exe and release\WinDeck-Portable-x.y.z.zip
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0release.ps1"
pause
