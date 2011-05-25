JScript, VBScript and COM clients can access the ImageResizer.dll if it is registered.

1) Run registerdll.bat on the server

2) Create an instance of ImageResizer.Configuration.Config using new ActiveXObject() or CreateObject()

3) Call .BuildImage on that instance, passing 3 variables: the source path, the destination path, and the querystring (settings)
