PhotoshopFileType Source Code Readme

Prerequisites
-------------
1. Windows XP or Windows Server 2003, or newer. You might be fine with Windows
   2000 but this hasn't been tested. Compiling in Windows Vista also hasn't been tested.

2. Visual C# 2005 or Visual Studio .NET 2005

3. .NET Framework 2.0

4. A installation of Paint.NET 2.6 or higher
   Download and install this from:
   http://www.eecs.wsu.edu/paint.net/redirect/getpdn.html


Instructions
------------
1. Open PhotoShopTest.sln with Microsoft Visual C# 2005 or Visual Studio .NET 2005. 

2. Copy the files "PaintDotNet.Data.dll" and "PdnLib.dll" from the Paint.NET 
   installation directory into PhotoShopFileType\lib.

3. Make sure the project configuration is set to "Release"
   This can be done by going to the "Build" menu, selecting "Configuration
   Manager...", selecting "Release" under "Active Solution 
   Configuration:" and then clicking Close.
    
4. Go to the "Build" menu and click "Rebuild Solution."

5. Assuming all went well, the output files are now in bin\Release:

   * PhotoShop.dll
     This is the file type plugin. Simply put it in the "FileTypes" folder 
     in the Paint.NET directory.
     

Directory Layout
----------------
PhotoShopFileType/
    This is the real plugin source code.

PsdFile/
    This is the source code for reading and writing the PSD files. 
    This is usable without Pain.NET

PsdFileTest/
    This is the source code for a test application wich displays the structure 
    of the psd file. It also decodes the Images into Bitmaps.
