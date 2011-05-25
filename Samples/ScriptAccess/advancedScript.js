var c = new ActiveXObject("ImageResizer.Configuration.Config");

var p = new ActiveXObject("ImageResizer.Plugins.PrettyGifs.PrettyGifs");

p.Install(c);

//c.setConfigXmlText("<resizer><plugins><add name=\"PrettyGifs\" /></plugins></resizer>");

//c.Plugins.LoadPlugins()

c.BuildImage("..\\Images\\quality-original.jpg","grass.gif", "rotate=3&width=600&format=gif&colors=128");
c.WriteDiagnosticsTo("advancedScript-1.txt");


var c2 = new ActiveXObject("ImageResizer.Configuration.Config");

c2.BuildImage("..\\Images\\quality-original.jpg","grass-ugly.gif", "rotate=3&width=600&format=gif");
c2.WriteDiagnosticsTo("advancedScript-2.txt");