%SystemRoot%\Microsoft.NET\Framework\v2.0.50727\regasm.exe ..\..\dlls\release\ImageResizer.dll /tlb:"%~dp0\typelibrary.tlb"

@echo.
@echo.
@echo Press a key to open typelibrary.tlb in OleVIew (requires visual studio 2010)
@echo it can also be downloaded from
@echo http://www.microsoft.com/downloads/en/details.aspx?FamilyID=5233B70D-D9B2-4CB5-AEB6-45664BE858B6
@pause

@"%PROGRAMFILES%\Microsoft SDKs\Windows\v7.0A\Bin\oleview.exe" typelibrary.tlb

@pause