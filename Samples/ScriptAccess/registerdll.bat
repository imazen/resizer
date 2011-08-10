@echo off
set batchdir=%~dp0
cd /d %batchdir%
cd ..
cd ..
cd dlls
cd release

set regasm32=%SystemRoot%\Microsoft.NET\Framework\v2.0.50727\regasm.exe
set regasm64=%SystemRoot%\Microsoft.NET\Framework64\v2.0.50727\regasm.exe

set destdir=%PROGRAMFILES%\ImageResizingNet\v3

IF NOT EXIST "%destdir%\ImageResizer.dll" GOTO clean

echo Potentially older DLLS already exist in %destdir%
echo Continuing will cause them to be unregistered so new ones can safely be installed

pause
REM set /P cunreg=Press 'y' to unregister them (suggested)
REM IF NOT ""%cunreg%""=="y" goto skipunregister

@echo on
@FOR /f %%G IN ('dir /B "%destdir%\ImageResizer*.dll"') DO %regasm32% "%destdir%\%%G" /u /codebase /nologo
@IF EXIST %regasm64% FOR /F %%G IN ('dir /B "%destdir%\ImageResizer*.dll"') DO %regasm64% "%destdir%\%%G" /u /codebase /nologo
@echo off

:skipunregister

echo .
echo Files to be deleted
dir /b %destdir%

echo .
set /P cdel=Press 'y' to delete the aforementioned files from %destdir%
IF NOT ""%cdel%""==""y"" goto clean

del %destdir%\*

:clean

Echo .
Echo . 
Echo Ready for installation
Echo Files to be copied:


dir /B *

echo Copy from: %cd%
echo To: %destdir%
set /P confirm=Press 'y' to copy the new files
IF NOT ""%confirm%""==""y"" goto register
@echo on
IF NOT EXIST "%destdir%" mkdir "%destdir%"

xcopy * "%destdir%\" /Y /R

@echo off
:register
Echo .
Echo Assemblies to register: 
dir /B "%destdir%\ImageResizer*.dll"

echo You may safely ignore all Codebase warnings below
set /P confirm=Press 'y' to register aforementioned assemblies
IF NOT ""%confirm%""==""y"" goto end

@echo on

@FOR /f %%G IN ('dir /B  "%destdir%\ImageResizer*.dll"') DO %regasm32% "%destdir%\%%G" /codebase /nologo
@IF EXIST %regasm64% FOR /F %%G IN ('dir /B  "%destdir%\ImageResizer*.dll"') DO %regasm64% "%destdir%\%%G" /codebase /nologo
@echo off

:end
echo The end..
pause 

cd /d %batchdir%