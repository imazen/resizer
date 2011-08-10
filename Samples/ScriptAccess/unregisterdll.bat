@echo off
set batchdir=%~dp0
cd /d %batchdir%
cd ..
cd ..
cd dlls
cd debug

set regasm32=%SystemRoot%\Microsoft.NET\Framework\v2.0.50727\regasm.exe
set regasm64=%SystemRoot%\Microsoft.NET\Framework64\v2.0.50727\regasm.exe

echo Path:%cd%
dir /B ImageResizer*.dll

pause


FOR /f %%G IN ('dir /B ImageResizer*.dll') DO %regasm32% "%cd%\%%G" /codebase /u

IF EXIST %regasm64% FOR /F %%G IN ('dir /B ImageResizer*.dll') DO %regasm64% "%cd%\%%G" /codebase .u

pause 