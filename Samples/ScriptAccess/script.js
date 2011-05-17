var c = new ActiveXObject("ImageResizer.Configuration.Config");

var s = new ActiveXObject("ImageResizer.ResizeSettings");




var b = c.CurrentImageBuilder;

c.Build("tractor-tiny.jpg","tractor2.jpg", s);