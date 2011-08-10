var dllsPath = "..\\..\\dlls\\release"; //The path the dlls are located relative to the script.

var f = new ActiveXObject("Scripting.FileSystemObject");
var shell = new ActiveXObject("WScript.Shell");
var sys = shell.Environment( "SYSTEM" );

//Destination folder
var destDir = sys("PROGRAMFILES") + "\\ImageResizingNet\\v3";

var irSourceDll = dllsPath + "\\ImageResizer.dll";
//If the new files can't be found, quit
if (!f.FileExists(irSourceDll)){
	WScript.Echo ("Please do not move this script from its original location. Cannot find " + itSourceDll + "   Exiting.");
	WScript.Quit(1);
}
//If the old version exists, what version is it?
var newVer = f.GetFileVersion(irSourceDll);
var oldVer = null;
if (f.FileExists(destDir + "\\ImageResizer.dll"))
	oldVer = f.GetFileVersion(destDir + "\\ImageResizer.dll");

//Is the main class registered? 
var canCreate = false;
try{
	var temp = new ActiveXObject("ImageResizer.Configuration.Config");
	canCreate = true;
}catch{}

if (oldVer == null && canCreate == true){
	
}
if (oldVer == newVer)
	
	
// Create File System Object to write to file.
var fso = new ActiveXObject( "Scripting.FileSystemObject" );
// Create 'collection' variables to hold each of the environments
