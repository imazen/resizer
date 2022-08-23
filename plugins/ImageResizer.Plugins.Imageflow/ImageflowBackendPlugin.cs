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
    public class ImageflowBackendPlugin : BuilderExtension, IPlugin, IIssueProvider, IFileExtensionPlugin, IQuerystringPlugin
    {
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

                job.ResultInfo["result.ext"] = firstEncodeResult.PreferredExtension;
                job.ResultInfo["result.mime"] = firstEncodeResult.PreferredMimeType;
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
            //Don't get involved if we're specifically not wanted.
            var builderString = job.Instructions["builder"];
            if (!ShouldBuild(builderString)) return RequestedAction.None;

            var weCanOutputThis = job.Dest is Stream || job.Dest is string;
            if (!weCanOutputThis) return RequestedAction.None;


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
            return new[] {"mode", "anchor", "flip", "sflip",
                "quality", "zoom", "dpr", "crop", "cropxunits", "cropyunits",
                "w", "h", "width", "height", "maxwidth", "maxheight", "format",
                "srotate", "rotate", "stretch", "webp.lossless", "webp.quality",
                "f.sharpen", "f.sharpen_when", "down.colorspace", "bgcolor", 
                "jpeg_idct_downscale_linear", "watermark", "s.invert", "s.sepia", 
                "s.grayscale", "s.alpha", "s.brightness", "s.contrast", "s.saturation", 
                "trim.threshold", "trim.percentpadding", "a.balancewhite",  "jpeg.progressive",
                "decoder.min_precise_scaling_ratio", "scale", "preset", "s.roundcorners", "ignoreicc" };
        }
    }
}