

# So you're not using ImageResizer's URL API?

To be clear, ImageResizer's URL API is our primary focus. 

If you're using ImageResizer via COM, [read the COM installation guide](/docs/v3/install/com).

If you are using the managed APIs, read ahead.

Outside the context of an ASP.NET application, you will need to do all configuration via code instead of XML. Standard Web.config installation only works inside an ASP.NET web application or website. 

.NET Console, WinForms, WCF, and WPF apps do *NOT* load all assemblies in '/bin' by default. You need to have in-code references to a plugin in order for it to load.

You start by creating an instance of `ImageResizer.Configuration.Config`. Then create an instance of each plugin you want to install, set any applicable settings on it, and then call `.Install(myConfigInstance)` in it.

``` 

var config = new ImageResizer.Configuration.Config();

new PrettyGifs().Install(config);

var job = new ImageJob("source", "destination", new Instructions("width=100"));

config.Build(job);

```

That's all, folks. 