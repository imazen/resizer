Aliases: /docs/plugins/basics /docs/extend /docs/extend/basics

# Making a plugin

Plugins are very easy to make and register. Here's a simple one that does nothing but support installation and uninstallation. All plugins need to do at least this much.

    namespace MyNamespace{
      public class SimplePlugin: IPlugin{
          public IPlugin Install(Configuration.Config c) {
              c.Plugins.add_plugin(this);
              return this;
          }

          public bool Uninstall(Configuration.Config c) {
              c.Plugins.remove_plugin(this);
              return true; //True for successful uninstallation, false if we couldn't do a clean job of it.
          }
      }
    }

You can install your own plugin just like any other - make sure it's referenced in the project, then put `<add name="MyNamespace.SimplePlugin"/>` into the `<plugins>` section of web.config. 
  
Or, you can install it during application start using `new SimplePlugin().Install(Config.Current);`, just like any other plugin. 

In V4 and later, you must specify the fully qualified name in web.config, i.e. `MyNamespace.MyPlugin, MyAssembly`. 

See the [Gradient plugin](/plugins/gradient) for an example of a short, yet very capable plugin.