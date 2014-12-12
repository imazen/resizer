@echo off

set fake=..\Packages\FAKE.3.11.0\tools\Fake
set fsx=FakeBuilder\Build.fsx

set fb_nuget_url=
set fb_nuget_key=
set fb_s3_id=
set fb_s3_key=
set fb_s3_bucket=

shift
2>nul call :case_%*
if errorlevel 1 call :case_help
exit /b

:case_clean
  %fake% %fsx% Clean
  goto end_case
:case_build
  %fake% %fsx% Build
  goto end_case
:case_rebuild
  %fake% %fsx% Custom targets=Clean;Build
  goto end_case
:case_pack_zips
  %fake% %fsx% Custom targets=PackZips;PrintInfo
  goto end_case
:case_pack_nuget
  %fake% %fsx% PackNuget
  goto end_case
:case_pack
  %fake% %fsx% Custom targets=PackNuget;PackZips;PrintInfo
  goto end_case
:case_push_zips
  set fb_s3_id=%1
  set fb_s3_key=%2
  set fb_s3_bucket=%3
  %fake% %fsx% PushS3
  goto end_case
:case_push_nuget
  set fb_nuget_url=%2
  set fb_nuget_key=%1
  %fake% %fsx% PushNuget
  goto end_case
:case_update
  echo - Fetching Submodules...
  git submodule init
  git submodule update
  echo - Running Restore...
  nuget restore ..\AppVeyor.sln
  echo - Fetching extra packaeges...
  nuget restore FakeBuilder\packages.config
  goto end_case
:case_help
  echo Usage:
  echo.
  echo builder update      - fetches git submodules, nuget packages, fake
  echo builder help        - shows this message
  echo.
  echo builder clean
  echo builder build
  echo builder rebuild     - clean and build
  echo.
  echo builder pack_zips
  echo builder pack_nuget
  echo builder pack        - pack zips and nuget
  echo.
  echo builder push_zips <s3_id> <s3_key> <s3_bucket>
  echo builder push_nuget <nuget_key> [nuget_feed]
  goto end_case
:end_case
  ver > nul
  goto :EOF
