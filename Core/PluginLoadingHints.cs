using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer.Configuration
{
    internal class PluginLoadingHints
    {
        private Dictionary<string, List<string>> hints;

        public PluginLoadingHints()
        {
            hints = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            LoadHints();
        }

        private void AddHint(string name, string expansion)
        {
            List<string> options = null;
            if (!hints.TryGetValue(name, out options) || options == null)
            {
                hints[name] = options = new List<string>();
            }
            options.Add(expansion);

        }
        public IEnumerable<string> GetExpansions(string name)
        {
            List<string> options = null;
            if (hints.TryGetValue(name, out options)) return options;
            else return null;
        }

        private void LoadHints(){
            

            //All plugins found in ImageResizer.dll under the ImageResizer.Plugins.Basic namespace
            foreach (string basic in new []{"AutoRotate", "ClientCache", "DefaultEncoder", "DefaultSettings", "Diagnostic", "DropShadow",
                            "FolderResizeSyntax", "Gradient", "IEPngFix", "Image404", "ImageHandlerSyntax", "ImageInfoAPI", 
                             "NoCache", "Presets", "SizeLimiting", "SpeedOrQuality", "Trial", "VirtualFolder"})
                AddHint(basic, "ImageResizer.Plugins.Basic." + basic + ", ImageResizer");

            //Except this one
            AddHint("MvcRoutingShim", "ImageResizer.Plugins.Basic.MvcRoutingShimPlugin, ImageResizer");
            //Plugins that don't use the Plugin class name suffix.
            foreach (string normalNoSuffix in new[] { "AdvancedFilters", "AnimatedGifs", "DiskCache", "PrettyGifs", "PsdReader", 
                "S3Reader2","SimpleFilters", })
                AddHint(normalNoSuffix, "ImageResizer.Plugins." + normalNoSuffix + "." + normalNoSuffix + ", ImageResizer.Plugins." + normalNoSuffix);
            
            //Plugins that use the Plugin suffix in the class name.
            foreach (string normalWithSuffix in new[] { "AzureReader2", "CloudFront", "CopyMetadata","CustomOverlay", "DiagnosticJson",
                "Faces", "FFmpeg", "Logging", "MongoReader", "PdfRenderer", "PsdComposer", "RedEye","RemoteReader", "SeamCarving",
                "SqlReader", "TinyCache", "Watermark", "WhitespaceTrimmer", "FastScaling"})
                AddHint(normalWithSuffix, "ImageResizer.Plugins." + normalWithSuffix + "." + normalWithSuffix + "Plugin, ImageResizer.Plugins." + normalWithSuffix);

            //Add 3 other plugins inside DiskCache
            foreach (string s in new[] { "SourceDiskCache", "SourceMemCache", "MemCache" })
                AddHint(s, "ImageResizer.Plugins." + s + "." + s + "Plugin, ImageResizer.Plugins.DiskCache");

            //Etags is in CustomOverlay still
            AddHint("Etags", "ImageResizer.Plugins.Etags.EtagsPlugin, ImageResizer.Plugins.CustomOverlay");
            
            //CropAround is inside Faces
            AddHint("CropAround", "ImageResizer.Plugins.CropAround.CropAroundPlugin, ImageResizer.Plugins.Faces");

            //Add 5 plugins inside FreeImage
            foreach (string s in new[] { "FreeImageBuilder", "FreeImageDecoder","FreeImageEncoder","FreeImageScaler","FreeImageResizer"})
                AddHint(s, "ImageResizer.Plugins." + s + "." + s + "Plugin, ImageResizer.Plugins.FreeImage");

            //Add 2 plugins inside WebP
            foreach (string s in new[] { "WebPDecoder", "WebPEncoder" })
                AddHint(s, "ImageResizer.Plugins." + s + "." + s + "Plugin, ImageResizer.Plugins.WebP");
            //Add 3 plugins inside WIC
            foreach (string s in new[] { "WicBuilder", "WicEncoder", "WicDecoder" })
                AddHint(s, "ImageResizer.Plugins." + s + "." + s + "Plugin, ImageResizer.Plugins.WIC");

            AddHint("Encrypted", "ImageResizer.Plugins.Encrypted.EncryptedPlugin, ImageResizer.Plugins.Security");

        }
    }
}
