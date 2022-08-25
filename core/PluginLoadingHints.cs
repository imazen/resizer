// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;

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
            if (!hints.TryGetValue(name, out options) || options == null) hints[name] = options = new List<string>();
            options.Add(expansion);
        }

        public IEnumerable<string> GetExpansions(string name)
        {
            List<string> options = null;
            if (hints.TryGetValue(name, out options)) return options;
            else return null;
        }

        public IDictionary<string, string> GetReverseHints()
        {
            var dict = new Dictionary<string, string>();
            foreach (var pair in hints.SelectMany(p => p.Value.Select(v => new KeyValuePair<string, string>(v, p.Key))))
            {
                var key = pair.Key;
                //Truncate the assembly name
                var comma = key.IndexOf(',');
                if (comma > -1) key = key.Substring(0, comma);
                if (!dict.ContainsKey(key)) dict[key] = pair.Value;
            }

            return dict;
        }

        private void LoadHints()
        {
            AddHint("Imageflow", "ImageResizer.Plugins.Imageflow.ImageflowBackendPlugin, ImageResizer.Plugins.Imageflow");
            AddHint("AzureReader2", "ImageResizer.Plugins.AzureReader2.AzureReader2Plugin, ImageResizer.Plugins.AzureReader2");
            AddHint("S3Reader2", "ImageResizer.Plugins.S3Reader2.S3Reader2Plugin, ImageResizer.Plugins.S3Reader2");
            AddHint("HybridCache", "ImageResizer.Plugins.HybridCache.HybridCachePlugin, ImageResizer.Plugins.HybridCache");
            AddHint("RemoteReader", "ImageResizer.Plugins.RemoteReader.RemoteReaderPlugin, ImageResizer.Plugins.RemoteReader");


            //All plugins found in ImageResizer.dll under the ImageResizer.Plugins.Basic namespace
            foreach (var basic in new[]
                     {
                         "AutoRotate", "LicenseDisplay", "ClientCache", "DefaultEncoder", "DefaultSettings",
                         "Diagnostic", "DropShadow", "DiagnoseRequest",
                         "FolderResizeSyntax", "Gradient", "IEPngFix", "Image404", "ImageHandlerSyntax", "ImageInfoAPI",
                         "NoCache", "Presets", "SizeLimiting", "SpeedOrQuality", "Trial", "VirtualFolder",
                         "WebConfigLicenseReader"
                     })
                AddHint(basic, "ImageResizer.Plugins.Basic." + basic + ", ImageResizer");


            //Except this one
            AddHint("MvcRoutingShim", "ImageResizer.Plugins.Basic.MvcRoutingShimPlugin, ImageResizer");
            //Plugins that don't use the Plugin class name suffix.
            foreach (var normalNoSuffix in new[]
                     {
                         "AdvancedFilters", "AnimatedGifs", "DiskCache", "PrettyGifs", "PsdReader",
                         "S3Reader2", "SimpleFilters"
                     })
                AddHint(normalNoSuffix,
                    "ImageResizer.Plugins." + normalNoSuffix + "." + normalNoSuffix + ", ImageResizer.Plugins." +
                    normalNoSuffix);

            //Plugins that use the Plugin suffix in the class name.
            foreach (var normalWithSuffix in new[]
                     {
                         "AzureReader2", "CloudFront", "CopyMetadata", "CustomOverlay", "DiagnosticJson",
                         "Faces", "FFmpeg", "Logging", "MongoReader", "PdfRenderer", "PdfiumRenderer", "PsdComposer",
                         "RedEye", "RemoteReader", "SeamCarving",
                         "SqlReader", "TinyCache", "Watermark", "WhitespaceTrimmer", "FastScaling"
                     })
                AddHint(normalWithSuffix,
                    "ImageResizer.Plugins." + normalWithSuffix + "." + normalWithSuffix +
                    "Plugin, ImageResizer.Plugins." + normalWithSuffix);

            //Add 3 other plugins inside DiskCache
            foreach (var s in new[] { "SourceDiskCache", "SourceMemCache", "MemCache" })
                AddHint(s, "ImageResizer.Plugins." + s + "." + s + "Plugin, ImageResizer.Plugins.DiskCache");

            //Etags is in CustomOverlay still
            AddHint("Etags", "ImageResizer.Plugins.Etags.EtagsPlugin, ImageResizer.Plugins.CustomOverlay");
            
            
            //CropAround is inside Faces
            AddHint("CropAround", "ImageResizer.Plugins.CropAround.CropAroundPlugin, ImageResizer.Plugins.Faces");

            //Add 5 plugins inside FreeImage
            foreach (var s in new[]
                     {
                         "FreeImageBuilder", "FreeImageDecoder", "FreeImageEncoder", "FreeImageScaler",
                         "FreeImageScaling", "FreeImageResizer"
                     })
                AddHint(s, "ImageResizer.Plugins." + s + "." + s + "Plugin, ImageResizer.Plugins.FreeImage");

            //Add 2 plugins inside WebP
            foreach (var s in new[] { "WebPDecoder", "WebPEncoder" })
                AddHint(s, "ImageResizer.Plugins." + s + "." + s + "Plugin, ImageResizer.Plugins.WebP");
            //Add 3 plugins inside WIC
            foreach (var s in new[] { "WicBuilder", "WicEncoder", "WicDecoder" })
                AddHint(s, "ImageResizer.Plugins." + s + "." + s + "Plugin, ImageResizer.Plugins.WIC");

            AddHint("Encrypted", "ImageResizer.Plugins.Encrypted.EncryptedPlugin, ImageResizer.Plugins.Security");
        }
    }
}