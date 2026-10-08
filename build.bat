@echo off
rem Double-click to build dist\WinDeck.exe
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
pause
