@echo off
pushd %~dp0

set fake=..\Packages\FAKE.3.11.0\tools\Fake
set fsx=FakeBuilder\Build.fsx


if [%1]==[] goto help
if [%1]==[help] goto help
if [%1]==[update] goto update
set target=%*

set target=%target:rebuild=clean;build%
set target=%target:pack_all=pack_nuget;pack_zips;print_stats%


:loop
shift
set snd=%2
set substr=%snd:~0,4%

if [%0]==[] goto end
if [%0]==[push_zips] (
  shift & set fb_s3_id=%1
  shift & set fb_s3_key=%2
  shift & set fb_s3_bucket=%3)
if [%0]==[push_nuget] (
  if "%substr%"=="http" (shift & set fb_nuget_url=%2)
  shift & set fb_nuget_key=%1)

goto loop
:end


%fake% %fsx% custom "targets=%target%"
goto exit


:update
  echo - Fetching Submodules...
  git submodule init
  git submodule update
  echo - Running Restore...
  nuget restore ..\AppVeyor.sln
  echo - Fetching extra packaeges...
  nuget restore FakeBuilder\packages.config
  goto exit


:help
  echo Usage:
  echo.
  echo builder ^<command^> [args]
  echo builder ^<command^>;[command];[command];...
  echo.
  echo A single fake call will be used for the multi-command intarface
  echo Multi-command can't use help/update
  echo.
  echo Commands:
  echo.
  echo update      - fetches git submodules, nuget packages, fake
  echo help        - shows this message
  echo clean
  echo build
  echo rebuild     - clean and build
  echo test
  echo pack_zips   - pack commands will delete old files before running
  echo pack_nuget
  echo pack_all    - pack zips and nuget
  echo push_zips ^<s3_id^> ^<s3_key^> ^<s3_bucket^>
  echo push_nuget ^<nuget_key^> [nuget_feed=nuget.org]
rem  echo patch_commit [hash=cur_head]
rem  echo patch_ver ^<filever^> ^<nugetver^> ^<infover^> [assemblyver=4.0.0.0]
  goto exit

:exit
  popd
  exit /b
