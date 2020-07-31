@echo off
pushd %~dp0

set fake=.\FakeBuilder\packages\FAKE\tools\Fake
set fsx=FakeBuilder\Build.fsx




if [%1]==[] goto help
if [%1]==[help] goto help
if [%1]==[prepare] goto prepare
set target=%*

set target=%target:rebuild=clean;build%
set target=%target:pack_all=pack_nuget;pack_zips;print_stats%


%fake% %fsx% custom "targets=%target%"
goto exit


:prepare

  echo - Running Restore...
  nuget restore ..\AppVeyor.sln

  nuget restore ..\Plugins\FastScaling\ImageResizer.Plugins.FastScaling.sln

  echo - Fetching extra packages...
  nuget restore FakeBuilder\packages.config

  echo - Fetching packages for build
  ..\.paket\paket.bootstrapper.exe
  if errorlevel 1 (
    exit /b %errorlevel%
  )

  cd .\FakeBuilder
  ..\..\.paket\paket.exe restore

  if errorlevel 1 (
    cd ..
    exit /b %errorlevel%
  )
  cd ..
  goto exit


:help
  echo Usage:
  echo.
  echo builder ^<command^>
  echo builder ^<command^>;[command];[command];...
  echo.
  echo A single fake call will be used for the multi-command interface
  echo Multi-command calls can't use help/prepare
  echo.
  echo Commands:
  echo.
  echo prepare              - fetches  nuget packages, fake
  echo help                - shows this message
  echo clean
  echo build
  echo rebuild             - clean and build
  echo test
  echo pack_zips           - pack commands will delete old files before running
  echo pack_nuget
  echo pack_all            - pack zips and nuget
  echo push_zips           - env vars: fb_s3_bucket fb_s3_id fb_s3_key
  echo push_nuget          - env vars: fb_nuget_url fb_nuget_key
  echo patch_info
  echo release ^<semver^>    - marks for publishing as release, ver must match git tag
  echo.
  echo Release env vars: fb_s3_rel_bucket, fb_s3_rel_id, fb_s3_rel_key,
  echo                   fb_nuget_rel_url, fb_nuget_rel_key
  goto exit

:exit
  popd
  exit /b
