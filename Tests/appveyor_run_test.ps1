param([string]$assembly, [switch]$run32bit)

$ErrorActionPreference = "Stop"
cd Packages/xunit.runners*/Tools


$xml_out_file = ".\xunit-results-$($assembly)-$($run32bit).xml"

$dll_path = "..\..\..\Tests\binaries\release\$($assembly).dll"

$runner = ".\xunit.console.exe"
switch ($run32bit){
	$true { $runner = ".\xunit.console.x86.exe"; break }
}


& "$($runner)"  $dll_path "-appveyor"
$return = $LastExitCode

# "-xmlv1" "$($xml_out_file)"


# upload results to AppVeyor
# $wc = New-Object 'System.Net.WebClient'
# $wc.UploadFile("https://ci.appveyor.com/api/testresults/xunit/$($env:APPVEYOR_JOB_ID)", (Resolve-Path $xml_out_file))

cd ..\..\..\
exit $return
