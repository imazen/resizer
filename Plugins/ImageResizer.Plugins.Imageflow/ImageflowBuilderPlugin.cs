using System.Collections.Specialized;
using ImageResizer.Configuration;
using ImageResizer.Configuration.Issues;
using ImageResizer.ExtensionMethods;
using ImageResizer.Resizing;

namespace ImageResizer.Plugins.Imageflow
{
    public class ImageflowBuilderPlugin : BuilderExtension, IPlugin, IIssueProvider, IFileExtensionPlugin
    {
        private bool something = false;
        public ImageflowBuilderPlugin()
        {
           
        }
        public ImageflowBuilderPlugin(NameValueCollection args) {
            something =  args.Get<bool>("someSetting", false);
        }
        
        Config c;
        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            this.c = c;
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }


        
    }
}