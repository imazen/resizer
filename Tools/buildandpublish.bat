@echo off

echo - Updating FAKE...
nuget Install FAKE -ExcludeVersion && echo - Done! || goto fail
nuget Install SharpZipLib -ExcludeVersion && echo - Done! || goto fail

echo - Gathering settings...
set fb_nuget_url=
set fb_nuget_key=

set /p q=Publish to a nuget server (y/n)?: 
if /i {%q%}=={n} (goto :no) 
set /p fb_nuget_url=Nuget URL (blank for nuget.org): 
set /p fb_nuget_key=Nuget API key: 
:no 

echo - Executing build.fsx
..\packages\FAKE\tools\Fake FakeBuilder\build.fsx && echo - Done! || goto fail

pause
exit /b 0

:fail
echo Something went wrong :/
pause
exit /b 1
