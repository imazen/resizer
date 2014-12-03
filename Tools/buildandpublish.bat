@echo off

echo - Running Restore...
nuget restore ..\AppVeyor.sln

echo - Fetching extra packaeges...
nuget Install FAKE -ExcludeVersion && echo - Done! || goto fail
nuget Install SharpZipLib -ExcludeVersion && echo - Done! || goto fail

echo - Gathering settings...

set fb_nuget_url=
set fb_nuget_key=
set fb_s3_url=
set fb_s3_key=
set fb_s3_bucket=

set /p q=Publish to a nuget server (y/n)?: 
if /i {%q%}=={n} (goto :no) 
set /p fb_nuget_url=Nuget URL (blank for nuget.org): 
set /p fb_nuget_key=Nuget API key: 
:no

set /p q=Push to S3 (y/n)?: 
if /i {%q%}=={n} (goto :no2) 
set /p fb_s3_url=S3 id: 
set /p fb_s3_key=S3 key: 
set /p fb_s3_bucket=S3 bucket: 
:no2

echo - Executing Build.fsx...
..\packages\FAKE\tools\Fake FakeBuilder\Build.fsx && echo - Done! || goto fail

pause
exit /b 0

:fail
echo Something went wrong :/
pause
exit /b 1
