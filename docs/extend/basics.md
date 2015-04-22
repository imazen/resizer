Aliases: /docs/plugins/basics /docs/extend /docs/extend/basics

# Making a plugin

Plugins are very easy to make and register. Here's a simple one that does nothing but support installation and uninstallation. All plugins need to do at least this much.

	public class SimplePlugin: IPlugin{
	    public IPlugin Install(Configuration.Config c) {
	        c.Plugins.add_plugin(this);
	        return this;
	    }

	    public bool Uninstall(Configuration.Config c) {
	        c.Plugins.remove_plugin(this);
	        return true; //True for successfull uninstallation, false if we couldn't do a clean job of it.
	    }
	}

You can install your own plugin just like any other - make sure it's referenced in the project, then put `<add name="Simple"/>` into the `<plugins>` section of web.config. 
	
Or, you can install it during application start using `new SimplePlugin().Install(Config.Current);`, just like any other plugin. 

If you aren't following the convention of naming your plugin `ImageResizer.Plugins.Simple.SimplePlugin`, you'll need to specify the fully qualified name in web.config, i.e. `MyNamespace.MySubNamespace.MyPlugin`. 

See the [Gradient plugin](/plugins/gradient) for an example of a short, yet very capable plugin.