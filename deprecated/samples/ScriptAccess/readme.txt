JScript, VBScript and COM clients can access the ImageResizer.dll (and plugins!) if they are registered.

1) Run dlls/release/COMInstaller.exe and click "Install".

2) Create an instance of ImageResizer.Configuration.Config using new ActiveXObject() or CreateObject()

3) Call .BuildImage on that instance, passing 3 variables: the source path, the destination path, and the querystring (settings)

4) You can use plugins by creating them and calling .Install(config):

 new ActiveXObject("ImageResizer.Plugins.PrettyGifs.PrettyGifs").Install(c);
 
See advancedScript.js for examples