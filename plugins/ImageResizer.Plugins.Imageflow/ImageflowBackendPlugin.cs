using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Imageflow.Fluent;
using ImageResizer.Configuration;
using Imazen.Common.Issues;
using ImageResizer.ExtensionMethods;
using ImageResizer.Resizing;

namespace ImageResizer.Plugins.Imageflow
{
    public class ImageflowBackendPlugin : BuilderExtension, IPlugin, IIssueProvider, IFileExtensionPlugin, IQuerystringPlugin, IPluginModifiesRequestCacheKey, IPluginSupportsOutputFileTypes
    {

        /// <summary>
        /// Change this whenever the logic around builder selection changes, or when imageflow does.
        /// </summary>
        private const string ImageflowCacheVersionKey = "1";
        
        private readonly ICollection<string> _supportedBuilderStrings =
            new List<string>(new[] { "wic", "freeimage", "imageflow" });

        private readonly bool _defaultBuilder = true;

        public ImageflowBackendPlugin()
        {
        }

        public ImageflowBackendPlugin(NameValueCollection args)
        {
            _defaultBuilder = args.Get<bool>("defaultBuilder", true);
            _supportedBuilderStrings = args.GetAsString("builderStrings", "imageflow,wic,freeimage")
                .Split(new[] { ',' }, StringSplitOptions.None);
        }

        public ImageFileType GuessOutputFileTypeIfSupported(Instructions commands, string virtualPath)
        {
            //TODO: review and test
            //The alternate implementation in DefaultEncoder is way more complicated
            
            var format = Path.GetExtension(virtualPath).Trim('.', ' ');
            if (!string.IsNullOrWhiteSpace(commands.Format))
            {
                format = commands.Format;
            }

            format = format.ToLowerInvariant();
            
            if (format == "webp")
            {
                //TODO: Technically we should only return this if the builder will actually handle the request
                
                return new ImageFileType() { MimeType = "image/webp", Extension = "webp" };
            }
            if (format == "jpg" || format == "jpeg")
            {
                return new ImageFileType() { MimeType = "image/jpeg", Extension = "jpg" };
            }
            if (format == "png")
            {
                return new ImageFileType() { MimeType = "image/png", Extension = "png" };
            }
            if (format == "gif")
            {
                return new ImageFileType() { MimeType = "image/gif", Extension = "gif" };
            }

            return null;
        }

        private Config c;

        public IPlugin Install(Config c)
        {
            c.Plugins.add_plugin(this);
            this.c = c;
            return this;
        }

        public bool Uninstall(Config c)
        {
            c.Plugins.remove_plugin(this);
            return true;
        }


        public IEnumerable<IIssue> GetIssues()
        {
            return Enumerable.Empty<IIssue>();
        }

        public IEnumerable<string> GetSupportedFileExtensions()
        {
            return Enumerable.Empty<string>();
        }

        private bool ShouldBuild(string builderString)
        {
            if (string.IsNullOrWhiteSpace(builderString) && _defaultBuilder) return true;

            return _supportedBuilderStrings.Any(val => val.Equals(builderString, StringComparison.OrdinalIgnoreCase));
        }


        private void ResolvePathAndCreateDir(string templatePath, ImageJob job, BuildDecodeResult firstDecodeResult,
            BuildEncodeResult result)
        {
            //Make physical and resolve variable references all at the same time.
            job.FinalPath = job.ResolveTemplatedPath(job.Dest as string,
                delegate(string var)
                {
                    if ("ext".Equals(var, StringComparison.OrdinalIgnoreCase)) return result.PreferredExtension;
                    if ("width".Equals(var, StringComparison.OrdinalIgnoreCase)) return result.Width.ToString();
                    if ("height".Equals(var, StringComparison.OrdinalIgnoreCase)) return result.Height.ToString();
                    return null;
                });
            //If requested, auto-create the parent directory(ies)
            if (job.CreateParentDirectory)
            {
                var dirName = Path.GetDirectoryName(job.FinalPath);
                if (dirName != null && !Directory.Exists(dirName))
                    Directory.CreateDirectory(dirName);
            }
        }

        private void DoJobWithImageflow(Stream source, ImageJob job)
        {
            //TODO: we should probably optimize this so instead of copying the unmanaged memory to a memory stream then
            //To the actual output, that we wrap it in StreamDestination

            // The command string we're passing to Imageflow
            var commandString = job.Instructions.ToQueryString().Trim('?');

            using (var imageflowJob = new global::Imageflow.Fluent.ImageJob())
            {
                var jobResult = imageflowJob.BuildCommandString(
                        new StreamSource(source, false),
                        new BytesDestination(), commandString) //TODO: Watermarks go here
                    .Finish()
                    //.SetSecurityOptions(options.JobSecurityOptions)
                    .InProcessAsync();

                var firstDecodeResult = jobResult.Result.DecodeResults.First();
                var firstEncodeResult = jobResult.Result.EncodeResults.First();

                job.ResultInfo["result.ext"] = firstEncodeResult.PreferredExtension ?? "jpg"; //Leaving this null causes the request to fail later;
                job.ResultInfo["result.mime"] = firstEncodeResult.PreferredMimeType ?? "image/jpeg"; //Leaving this null causes the request to fail later;
                job.ResultInfo["source.width"] = firstDecodeResult.Width;
                job.ResultInfo["source.height"] = firstDecodeResult.Height;
                job.ResultInfo["final.width"] = firstEncodeResult.Width;
                job.ResultInfo["final.height"] = firstEncodeResult.Height;

                var encodeBytesObject = firstEncodeResult.TryGetBytes();
                if (encodeBytesObject?.Array == null)
                    throw new InvalidOperationException("Encoded bytes not available");
                var encodeBytes = encodeBytesObject.Value;


                switch (job.Dest)
                {
                    case string dest:
                    {
                        ResolvePathAndCreateDir(dest, job, firstDecodeResult, firstEncodeResult);
                        using (var fs = new FileStream(job.FinalPath, FileMode.OpenOrCreate, FileAccess.Write,
                                   FileShare.None))
                        {
                            fs.Write(encodeBytes.Array ?? throw new InvalidOperationException(), encodeBytes.Offset,
                                encodeBytes.Count);
                        }

                        break;
                    }
                    case Stream dest:
                    {
                        var outputStream = dest;
                        outputStream.Write(encodeBytes.Array ?? throw new InvalidOperationException(),
                            encodeBytes.Offset, encodeBytes.Count);
                        break;
                    }
                    default:
                        throw new NotSupportedException("We should have filtered these out already");
                }
            }
        }

        /// <summary>
        ///     Adds alternate pipeline based on Imageflow.
        ///     This method doesn't handle job.DisposeSource or job.DesposeDest or settings filtering, that's handled by
        ///     ImageBuilder.
        ///     Handles all the work for turning 'source' into a byte[]/long pair.
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        protected override RequestedAction BuildJob(ImageJob job)
        {
            //!!Bump ImageflowCacheVersionKey whenever we change this logic!!
            //It's not part URL rewriting so it doesn't change the cache key otherwise
            
            //Don't get involved if we're specifically not wanted.
            var builderString = job.Instructions["builder"];
            if (!ShouldBuild(builderString)) return RequestedAction.None;

            var weCanOutputThis = job.Dest is Stream || job.Dest is string;
            if (!weCanOutputThis) return RequestedAction.None;
     
            //Imageflow doesn't support TIFF OR BMP files, so use the default builder.
            if (job.SourcePathData != null &&
                (job.SourcePathData.EndsWith(".tif", StringComparison.OrdinalIgnoreCase) ||
                 job.SourcePathData.EndsWith(".tiff", StringComparison.OrdinalIgnoreCase) ||
                 job.SourcePathData.EndsWith(".tff", StringComparison.OrdinalIgnoreCase) ||
                 job.SourcePathData.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)))
            {
                return RequestedAction.None;
            }
            //Imageflow doesn't support certain other querystring commands, so use the default builder for those, also
            var integerCommands = new[] { "paddingWidth", "paddingHeight", "margin", "borderWidth" };

            var usesBordersOrPadding = integerCommands
                .Select(key => job.Instructions[key])
                .Where(val => !string.IsNullOrWhiteSpace(val))
                .Any(val => int.TryParse(val, out var intVal) && intVal > 0);
            
            
            if (usesBordersOrPadding) return RequestedAction.None;

            //Imageflow doesn't support rotation except in intervals of 90.
            var rotationAngle = job.Instructions["rotate"];
            if (!string.IsNullOrWhiteSpace(rotationAngle))
            {
                if (int.TryParse(job.Instructions["rotate"], out var degrees) && degrees % 90 != 0)
                    return RequestedAction.None;
            } 
            //Imageflow doesn't support GIF frame selection
            if (!string.IsNullOrWhiteSpace(job.Instructions["frame"]))
            {
                return RequestedAction.None;
            }
            //Imageflow doesn't support paddingColor, just &bgcolor
            if (!string.IsNullOrWhiteSpace(job.Instructions["paddingColor"]))
            {
                return RequestedAction.None;
            }

            // Acquire the stream and handle its disposal and position as requested.
            Stream s = null;
            var disposeStream = !(job.Source is Stream);
            long originalPosition = 0;
            var restoreStreamPosition = false;
            try
            {
                //Get a Stream instance for the job
                s = c.CurrentImageBuilder.GetStreamFromSource(job.Source, new ResizeSettings(job.Instructions), ref disposeStream, out var path,
                    out restoreStreamPosition);
                if (s == null) return RequestedAction.None; //We don't support the source object!

                if (job.ResetSourceStream) restoreStreamPosition = true;
                // Store the file path associated with the stream
                job.SourcePathData = path;

                //Save the original stream position
                originalPosition = restoreStreamPosition ? s.Position : -1;

                //TODO: magic byte detection would be better; but we can't transfer the stream to the other builder
                // if (s.CanSeek)
                // {
                //     //TODO use await
                //
                //     var first12 = new byte[12];
                //     var bytesRead = s.Read(first12, 0, 12);
                //     s.Seek(0, SeekOrigin.Begin);
                //     if (bytesRead >= 12)
                //     {
                //         var type = new Imazen.Common.FileTypeDetection.FileTypeDetector().GuessMimeType(first12);
                //         if (type == "image/tiff") return RequestedAction.None;
                //     }
                //     
                // }

                DoJobWithImageflow(s, job);
                return RequestedAction.Cancel;
            }
            finally
            {
                if (s != null && restoreStreamPosition && s.CanSeek) s.Seek(originalPosition, SeekOrigin.Begin);
                if (disposeStream && s != null) s.Dispose();
            }
        }

        
        public IEnumerable<string> GetSupportedQuerystringKeys()
        {
            //Some commands are only modifier commands, and shouldn't trigger processing without other commands present.
            // return new[] {"mode", "anchor",  "cropxunits", "cropyunits", "stretch", 
            //     "down.colorspace","up.colorspace","jpeg_idct_downscale_linear", 
            //     "decoder.min_precise_scaling_ratio", "scale",  "ignoreicc" };

            return new[] { "flip", "sflip",
                "quality", "zoom", "dpr", "crop", 
                "w", "h", "width", "height", "maxwidth", "maxheight", "format",
                "srotate", "rotate",  "webp.lossless", "webp.quality",
                "watermark", "s.invert", "s.sepia", 
                "s.grayscale", "s.alpha", "s.brightness", "s.contrast", "s.saturation", 
                "trim.threshold", "trim.percentpadding", "a.balancewhite",  "jpeg.progressive",
                "preset", "s.roundcorners"};
        }

        public string ModifyRequestCacheKey(string currentKey, string virtualPath, NameValueCollection queryString)
        {
            return currentKey + "|imageflow" + ImageflowCacheVersionKey; //TODO: combine with a cache key breaker returned from imageflow.dll itself.
            // Simply by being installed it invalidates the old GDI results. This is very good. 
        }
    }
}