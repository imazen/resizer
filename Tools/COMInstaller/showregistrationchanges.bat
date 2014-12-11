@echo This script shows you what registry changes are made (or removed) when installing/uninstalling ImageResizer.dll using COMInstaller
%SystemRoot%\Microsoft.NET\Framework\v2.0.50727\regasm.exe ..\..\dlls\release\ImageResizer.dll /regfile:regfile.txt /codebase

notepad regfile.txt
pause