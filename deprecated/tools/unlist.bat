REM nuget.exe delete {your_package_id} {version} -Source {feed URL} -ApiKey key

SET EnableNugetPackageRestore=true

SET NUGET_API_KEY=

for /r %%i in (..\nuget\*.nuspec) do nuget delete %%~ni 4.1.2 -NonInteractive -Source https://www.nuget.org/api/v2/package -ApiKey %NUGET_API_KEY%

for /r %%i in (..\nuget\*.nuspec) do nuget delete %%~ni 4.1.1 -NonInteractive -Source https://www.nuget.org/api/v2/package -ApiKey %NUGET_API_KEY%