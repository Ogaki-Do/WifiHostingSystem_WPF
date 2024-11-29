@echo off
dir *.svg /b > filelist.txt
for /f "tokens=1 delims=." %%a in ( filelist.txt ) do (
C:\Program Files\WindowsApps\25415Inkscape.Inkscape_1.3.2.0_x64__9waqn51p1ttv2\VFS\ProgramFilesX64\Inkscape\bin\inkscape.exe -p %%a.svg -o %%a.eps
)
del filelist.txt
