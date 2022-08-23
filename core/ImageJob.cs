// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ImageResizer.ExtensionMethods;
using ImageResizer.Plugins;
using ImageResizer.Util;

namespace ImageResizer
{
    public class ImageJob
    {
        private class NullProfiler : IProfiler
        {
            public bool Active => false;

            public void Start(string segmentName, bool assertStopped = true)
            {
            }

            public bool IsRunning(string segmentName)
            {
                return false;
            }

            public void Stop(string segmentName, bool assertRunning = true, bool stopChildren = false)
            {
            }


            public void LogStart(long ticks, string segmentName, bool allowRecursion = false)
            {
                throw new NotImplementedException();
            }

            public void LogStop(long ticks, string segmentName, bool assertRunning = true, bool stopChildren = false)
            {
                throw new NotImplementedException();
            }
        }

        public ImageJob()
        {
            RequestedInfo = new List<string>();
            ResultInfo = new Dictionary<string, object>();
            Profiler = new NullProfiler();
        }

        public ImageJob(object source, object dest, Instructions instructions) : this()
        {
            Source = source;
            Dest = dest;
            Instructions = instructions;
        }

        public ImageJob(object source, object dest, Instructions instructions, bool disposeSource,
            bool addFileExtension)
            : this(source, dest, instructions)

        {
            DisposeSourceObject = disposeSource;
            AddFileExtension = addFileExtension;
        }

        public ImageJob(Stream sourceStream, Stream destStream, Instructions instructions)
            : this((object)sourceStream, (object)destStream, instructions)
        {
        }

        public ImageJob(string sourcePath, string destPath, Instructions instructions)
            : this((object)sourcePath, (object)destPath, instructions)
        {
        }

        /// <summary>
        ///     Creates an ImageJob that won't run a full build - it will only do enough work in order to supply the requested data
        ///     fields.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="requestedImageInfo">Pass null to use "source.width","source.height", "result.ext","result.mime". </param>
        public ImageJob(object source, IEnumerable<string> requestedImageInfo) : this()
        {
            Source = source;
            Dest = typeof(IDictionary<string, object>);
            RequestedInfo = new List<string>(requestedImageInfo == null
                ? new[] { "source.width", "source.height", "result.ext", "result.mime" }
                : requestedImageInfo);
            Instructions = new Instructions();
        }


        [Obsolete("Use Instructions instead of ResizeSettings")]
        public ImageJob(string sourcePath, string destPath, ResizeSettings settings)
            : this((object)sourcePath, (object)destPath, new Instructions(settings))
        {
        }

        [Obsolete("Use Instructions instead of ResizeSettings")]
        public ImageJob(Stream sourceStream, Stream destStream, ResizeSettings settings)
            : this((object)sourceStream, (object)destStream, new Instructions(settings))
        {
        }

        [Obsolete("Use Instructions instead of ResizeSettings")]
        public ImageJob(object source, object dest, ResizeSettings settings) :
            this(source, dest, new Instructions(settings))
        {
        }

        [Obsolete("Use Instructions instead of ResizeSettings")]
        public ImageJob(object source, object dest, ResizeSettings settings, bool disposeSource, bool addFileExtension)
            : this(source, dest, new Instructions(settings), disposeSource, addFileExtension)
        {
        }

        /// <summary>
        ///     Shorthand method for ImageBuilder.Current.Build(this)
        /// </summary>
        /// <returns></returns>
        public ImageJob Build()
        {
            return ImageBuilder.Current.Build(this);
        }

        /// <summary>
        ///     A list of strings which define properties that can be returned to the caller. "source.width", "source.height",
        ///     "result.ext", "result.mime" are the most commonly used. Defaults to none
        /// </summary>
        public List<string> RequestedInfo { get; set; }

        /// <summary>
        ///     A dictionary of key/value pairs provided along with the result.
        /// </summary>
        public Dictionary<string, object> ResultInfo { get; set; }

        private object _source = null;

        /// <summary>
        ///     The source image's physical path, app-relative virtual path, or a Stream, byte array, Bitmap, VirtualFile,
        ///     IVirtualFile, HttpPostedFile, or HttpPostedFileBase instance.
        /// </summary>
        public object Source
        {
            get => _source;
            set => _source = value;
        }

        private object _dest = null;

        /// <summary>
        ///     The destination Stream, physical path, or app-relative virtual path. If a Bitmap instance is desired,
        ///     set this to typeof(System.Drawing.Bitmap). The result will be stored in .Result
        /// </summary>
        public object Dest
        {
            get => _dest;
            set => _dest = value;
        }


        private object _result = null;

        /// <summary>
        ///     The result if a Bitmap, BitmapSource, or IWICBitmapSource instance is requested.
        /// </summary>
        public object Result
        {
            get => _result;
            set => _result = value;
        }

        /// <summary>
        ///     The width, in pixels, of the first frame or page in the source image file
        /// </summary>
        public int? SourceWidth => ResultInfo.Get<int?>("source.width", null);

        /// <summary>
        ///     The height, in pixels, of the first frame or page in the source image file
        /// </summary>
        public int? SourceHeight => ResultInfo.Get<int?>("source.height", null);

        /// <summary>
        ///     The width, in pixels, of the first frame or page in the final image file
        /// </summary>
        public int? FinalWidth => ResultInfo.Get<int?>("final.width", null);

        /// <summary>
        ///     The height, in pixels, of the first frame or page in the final image file
        /// </summary>
        public int? FinalHeight => ResultInfo.Get<int?>("final.height", null);


        /// <summary>
        ///     The correct file extension for the resulting file stream, without a leading dot. Will be null if the result is not
        ///     an encoded image.
        /// </summary>
        public string ResultFileExtension => ResultInfo.Get<string>("result.ext", null);

        /// <summary>
        ///     The correct mime type for the resulting file stream, without a leading dot. Will be null if the result is not an
        ///     encoded image.
        /// </summary>
        public string ResultMimeType => ResultInfo.Get<string>("result.mime", null);


        /// <summary>
        ///     Ticks elapsed during Job processing. Divide by Stopwatch.Frequency to get seconds.
        /// </summary>
        public long TotalTicks { get; set; }

        /// <summary>
        ///     Ticks elapsed during read and decode (not all engines populate this). Divide by Stopwatch.Frequency to get seconds.
        /// </summary>
        public long DecodeTicks { get; set; }

        /// <summary>
        ///     Ticks elapsed during encode and write (not all engines populate this). Divide by Stopwatch.Frequency to get
        ///     seconds.
        /// </summary>
        public long EncodeTicks { get; set; }

        /// <summary>
        ///     The image processing settings
        /// </summary>
        [Obsolete("Use Instructions instead.")]
        public ResizeSettings Settings
        {
            get => new ResizeSettings(Instructions);
            set => Instructions = new Instructions(value);
        }

        /// <summary>
        ///     The profiler to report start/stop events to.
        /// </summary>
        public IProfiler Profiler { get; set; }

        /// <summary>
        ///     The image processing instructions
        /// </summary>
        public Instructions Instructions { get; set; }


        public string InstructionsAsString
        {
            get => Instructions.ToQueryString();
            set => Instructions = new Instructions(value);
        }

        private bool _disposeSourceObject = true;

        /// <summary>
        ///     If true, and if 'source' is a IDisposable instead like Bitmap or Stream instance, it will be disposed after it has
        ///     been used. Defaults to true.
        /// </summary>
        public bool DisposeSourceObject
        {
            get => _disposeSourceObject;
            set => _disposeSourceObject = value;
        }

        private bool _resetSourceStream = false;

        /// <summary>
        ///     If true, and if 'source' is seekable, the stream will be reset to its previous position after being read.
        ///     Always true for HttpPostedFile(Base) instances, defaults to false for all others.
        /// </summary>
        public bool ResetSourceStream
        {
            get => _resetSourceStream;
            set => _resetSourceStream = value;
        }

        private bool _disposeDestinationStream = false;

        /// <summary>
        ///     If true, and if 'dest' is a Stream instance, it will be disposed after the image has been written. Defaults to
        ///     false.
        /// </summary>
        public bool DisposeDestinationStream
        {
            get => _disposeDestinationStream;
            set => _disposeDestinationStream = value;
        }

        private string _finalPath = null;

        /// <summary>
        ///     Contains the final physical path to the image (if 'dest' was a path - null otherwise)
        /// </summary>
        public string FinalPath
        {
            get => _finalPath;
            set => _finalPath = value;
        }


        private string _sourcePathData = null;

        /// <summary>
        ///     If 'source' contains any path-related data, it is copied into this member for use by format detection code, so
        ///     decoding can be optimized.
        ///     May be a physical or virtual path, or just a file name.
        /// </summary>
        public string SourcePathData
        {
            get => _sourcePathData;
            set => _sourcePathData = value;
        }

        private bool _addFileExtension = false;

        /// <summary>
        ///     If true, the appropriate extension for the encoding format will be added to the destination path, and the result
        ///     will be stored in FinalPath in physical path form.
        /// </summary>
        public bool AddFileExtension
        {
            get => _addFileExtension;
            set => _addFileExtension = value;
        }

        private bool _allowDestinationPathVariables = true;

        /// <summary>
        ///     If true (the default), destination paths can include variables that are expanded during the image build process.
        ///     Ex. Dest = "~/folder/&lt;guid>.&lt;ext>" will expand to "C:\WWW\App\folder\1ddadaadaddaa75da75ad34ad33da3a.jpg".
        /// </summary>
        public bool AllowDestinationPathVariables
        {
            get => _allowDestinationPathVariables;
            set => _allowDestinationPathVariables = value;
        }

        private bool _createParentDirectory = false;

        /// <summary>
        ///     Defaults to false. When true, the parent directory of the destination filename will be created if it doesn't
        ///     already exist.
        /// </summary>
        public bool CreateParentDirectory
        {
            get => _createParentDirectory;
            set => _createParentDirectory = value;
        }

        /// <summary>
        ///     Sets CreateParentDirectory to true. Provided for easy chaining so you can do one-liners.
        ///     new ImageJob(source,dest,settings).CreateDir().Build()
        /// </summary>
        /// <returns></returns>
        public ImageJob CreateDir()
        {
            CreateParentDirectory = true;
            return this;
        }

        /// <summary>
        ///     Internal use only.
        ///     Resolves the specified (potentially templated) path into a physical path.
        ///     Applies the AddFileExtension setting using the 'ext' variable.
        ///     Supplies the guid, settings.*, filename, path, and originalExt variables.
        ///     The resolver method should supply 'ext', 'width', and 'height' (all of which refer to the final image).
        ///     If AllowDestinationPathVariables=False, only AddFileExtension will be processed.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="resolver"></param>
        /// <returns></returns>
        public string ResolveTemplatedPath(string path, PathUtils.VariableResolverCallback resolver)
        {
            if (!AllowDestinationPathVariables)
                //Only add the extension if requested when variables are turned off.
                return PathUtils.MapPathIfAppRelative(path + (AddFileExtension ? "." + resolver("ext") : ""));
            if (AddFileExtension) path = path + ".<ext>";
            path = PathUtils.ResolveVariablesInPath(path, delegate(string p)
            {
                //Let the 'resolver' passed to this method take precedence - we provide default values.
                var result = resolver(p);
                if (result != null) return result;
                //GUID in lowercase hexadecimal with no hyphens
                if ("guid".Equals(p, StringComparison.OrdinalIgnoreCase))
                    return Guid.NewGuid().ToString("N", NumberFormatInfo.InvariantInfo);
                //Access to the settings collection
                var settingsPrefix = "settings.";
                if (p.StartsWith(settingsPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var subName = p.Substring(settingsPrefix.Length);
                    return Instructions[subName];
                }

                //Access to the instructions collection
                var instructionsPrefix = "instructions.";
                if (p.StartsWith(instructionsPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var subName = p.Substring(instructionsPrefix.Length);
                    return Instructions[subName];
                }

                if ("filename".Equals(p, StringComparison.OrdinalIgnoreCase))
                {
                    if (SourcePathData == null)
                        throw new ImageProcessingException(
                            "You cannot use the <filename> variable in a job that does not have a source filename, such as with a Stream or Bitmap instance");
                    return Path.GetFileNameWithoutExtension(SourcePathData);
                }

                if ("dir".Equals(p, StringComparison.OrdinalIgnoreCase))
                {
                    if (SourcePathData == null)
                        throw new ImageProcessingException(
                            "You cannot use the <dir> variable in a job that does not have a source filename, such as with a Stream or Bitmap instance");
                    return Path.GetDirectoryName(SourcePathData); //Just remove the last segment
                }

                if ("path".Equals(p, StringComparison.OrdinalIgnoreCase))
                {
                    if (SourcePathData == null)
                        throw new ImageProcessingException(
                            "You cannot use the <path> variable in a job that does not have a source filename, such as with a Stream or Bitmap instance");
                    return PathUtils.RemoveExtension(SourcePathData); //Just remove the last segment
                }

                if ("originalext".Equals(p, StringComparison.OrdinalIgnoreCase))
                {
                    if (SourcePathData == null)
                        throw new ImageProcessingException(
                            "You cannot use the <originalext> variable in a job that does not have a source filename, such as with a Stream or Bitmap instance");
                    return PathUtils.GetExtension(SourcePathData); //Just remove the last segment
                }


                return null;
            });
            return PathUtils.MapPathIfAppRelative(path);
        }
    }
}