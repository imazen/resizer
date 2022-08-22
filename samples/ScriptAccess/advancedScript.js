//The first step is to create a configuration object (where you register plugins, configure stuff, etc.)
var c = new ActiveXObject("ImageResizer.Configuration.Config");

//This is how you install a plugin
var p = new ActiveXObject("ImageResizer.Plugins.PrettyGifs.PrettyGifs");
p.Install(c);

//This is how you generate an image
c.BuildImage("..\\Images\\quality-original.jpg","grass.gif", "rotate=3&width=600&format=gif&colors=128");

// You can even get diagnostics output if something's not working
// c.WriteDiagnosticsTo("advancedScript-1.txt");

//You can create different configurations
var c2 = new ActiveXObject("ImageResizer.Configuration.Config");

//And use them separately
c2.BuildImage("..\\Images\\quality-original.jpg","grass-ugly.gif", "rotate=3&width=600&format=gif");

//c2.WriteDiagnosticsTo("advancedScript-2.txt");