@echo off
set batchdir=%~dp0
cd /d %batchdir%
cd ..
cd ..
cd dlls
cd release
set dllpath=%cd%\ImageResizer

echo Path: %dllpath%


pause
%SystemRoot%\Microsoft.NET\Framework\v2.0.50727\regasm.exe %dllpath%.dll /codebase
%SystemRoot%\Microsoft.NET\Framework\v2.0.50727\regasm.exe %dllpath%.Plugins.PrettyGifs.dll /codebase
%SystemRoot%\Microsoft.NET\Framework\v2.0.50727\regasm.exe %dllpath%.Plugins.RemoteReader.dll /codebase
pause 