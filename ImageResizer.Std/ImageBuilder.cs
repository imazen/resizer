// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.Licensed under the Apache License, Version 2.0.
using System;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using ImageResizer.Resizing;
using ImageResizer.Encoding;
using ImageResizer.Util;
using ImageResizer.Configuration;
using ImageResizer.Plugins;

namespace ImageResizer
{
    /// <summary>
    /// Provides methods for generating resized images, and for reading and writing them to disk.
    /// Use ImageBuilder.Current to get the current instance (as configured in the application configuration), or use ImageBuilder.Current.Create() to control which extensions are used.
    /// </summary>
    public partial class ImageBuilder : IQuerystringPlugin, IFileExtensionPlugin
    {
        /// <summary>
        /// Shouldn't be used except to make a factory instance.
        /// </summary>
        protected ImageBuilder() { }
        protected IEncoderProvider _encoderProvider = null;
        /// <summary>
        /// Handles the encoder selection and provision process.
        /// </summary>
        public IEncoderProvider EncoderProvider { get { return _encoderProvider; } }


        protected ISettingsModifier _settingsModifier = null;
        /// <summary>
        /// May be null. A class to modify or normalize ResizeSettings instances before they are used.
        /// </summary>
        public ISettingsModifier SettingsModifier { get { return _settingsModifier; } }

        private IVirtualImageProvider _virtualFileProvider;

        /// <summary>
        /// Provides a resolution service for app-relative URLs. 
        /// </summary>
        public IVirtualImageProvider VirtualFileProvider {
            get { return _virtualFileProvider; }
        }

        /// <summary>
        /// Returns a shared instance of ImageBuilder or a subclass, equivalent to  Config.Current.CurrentImageBuilder
        /// </summary>
        /// <returns></returns>
        public static ImageBuilder Current {get{ return Config.Current.CurrentImageBuilder; }}

        private IEnumerable<BuilderExtension> exts;
        /// <summary>
        /// Create a new instance of ImageBuilder using the specified extensions, encoder provider, file provider, and settings filter. Extension methods will be fired in the order they exist in the collection.
        /// </summary>
        /// <param name="extensions"></param>
        /// <param name="encoderProvider"></param>
        /// <param name="settingsModifier"></param>
        /// <param name="virtualFileProvider"></param>
        public ImageBuilder(IEnumerable<BuilderExtension> extensions, IEncoderProvider encoderProvider, IVirtualImageProvider virtualFileProvider, ISettingsModifier settingsModifier)
            {
            this.exts = extensions;
            this._encoderProvider = encoderProvider;
            this._virtualFileProvider = virtualFileProvider;
            this._settingsModifier = settingsModifier;
        }

        /// <summary>
        /// Creates another instance of the class using the specified extensions. Subclasses should override this and point to their own constructor.
        /// </summary>
        /// <param name="extensions"></param>
        /// <param name="writer"></param>
        /// <param name="virtualFileProvider"></param>
        /// <param name="settingsModifier"></param>
        /// <returns></returns>
        public virtual ImageBuilder Create(IEnumerable<BuilderExtension> extensions, IEncoderProvider writer, IVirtualImageProvider virtualFileProvider, ISettingsModifier settingsModifier) {
            return new ImageBuilder(extensions,writer,virtualFileProvider,settingsModifier);
        }
        /// <summary>
        /// Copies the instance along with extensions. Subclasses must override this.
        /// </summary>
        /// <returns></returns>
        public virtual ImageBuilder Copy(){
            return new ImageBuilder(this.exts,this._encoderProvider, this._virtualFileProvider,this._settingsModifier);
        }

        
        /// <summary>
        /// Returns a dictionary of information about the given image. 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="requestedInfo">Pass null to get the defaults ("source.width", source.height")</param>
        /// <returns></returns>
        public virtual IDictionary<string,object> LoadImageInfo(object source, IEnumerable<string> requestedInfo){
            return Build(new ImageJob(source, requestedInfo)).ResultInfo;
        }
        
        /// <summary>
        /// For plugin use only. 
        /// Returns a stream instance from the specified source object and settings object. 
        /// To extend this method, override GetStream.
        /// </summary>
        /// <param name="source">The physical or app-relative path, or a VirtualFile, IVirtualFile, Stream, HttpPostedFile, or HttpPostedFileBase instance.</param>
        /// <param name="settings">Querystring settings to pass to the VirtualFileProvider</param>
        /// <param name="disposeStream">You should externally initialize this to true, unless the user-provided 'source' is a Stream instance. Will be set to false for HttpPostedFile and HttpPostedFileBase instances, so they can be reused. </param>
        /// <param name="path">The physical or virtual path associated with the stream (if present). Otherwise null</param>
        /// <param name="restoreStreamPosition">True if you should save and restore the seek position of the stream. True for HttpPostedFile and HttpPostedFileBase instances. </param>
        /// <returns></returns>
        public Stream GetStreamFromSource(object source, ResizeSettings settings, ref bool disposeStream, out string path, out bool restoreStreamPosition) {
            if (source == null) throw new ArgumentNullException("source", "The source argument cannot be null; how do you load an image from a null value?");
            if (settings == null) settings = new ResizeSettings();

            //Allow plugins to extend this
            bool disposeS = disposeStream;
            Stream s = base.GetStream(source, settings, ref disposeS, out path, out restoreStreamPosition);
            if (s != null) {
                disposeStream = disposeS;
                return s;
            }

            //App-relative path - converted to virtual path
            if (source is string) {
                path = source as string;
                //Convert app-relative paths to VirtualFile instances
                if (path.StartsWith("~", StringComparison.OrdinalIgnoreCase)) {
                    string virtualPath = HostingEnvironment.ApplicationVirtualPath == null ? path.TrimStart('~') : PathUtils.ResolveAppRelative(path);
                    source = this.VirtualFileProvider.GetFile(virtualPath, settings);
                    if (source == null) throw new FileNotFoundException("The specified virtual file could not be found.", virtualPath);
                }
            }

            path = null;
            restoreStreamPosition = false;
            //Stream
            if (source is Stream) {
                s = (Stream)source;
            }
                //VirtualFile
            else if (source is System.Web.Hosting.VirtualFile) {
                path = ((System.Web.Hosting.VirtualFile)source).VirtualPath;
                s = ((System.Web.Hosting.VirtualFile)source).Open();
                //IVirtualFile
            } else if (source is IVirtualFile) {
                path = ((IVirtualFile)source).VirtualPath;
                s = ((IVirtualFile)source).Open();
                //PhysicalPath
            } else if (source is string) {
                path = (string)source;
                s = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            } else if (source is byte[]) {
                s = new MemoryStream((byte[])source, 0, ((byte[])source).Length, false, true);
            } else {
                //For HttpPostedFile and HttpPostedFileBase - we must use reflection to support .NET 3.5 without losing 2.0 compat.
                PropertyInfo pname = source.GetType().GetProperty("FileName", typeof(string));
                PropertyInfo pstream = source.GetType().GetProperty("InputStream");

                if (pname != null && pstream != null) {
                    path = pname.GetValue(source, null) as string;
                    s = pstream.GetValue(source, null) as Stream;
                    disposeStream = false; //We never want to dispose the HttpPostedFile or HttpPostedFileBase streams..
                    restoreStreamPosition = true;
                }

                if (s == null) return null;
            }


            try {

                if (s != null && s.Length <= s.Position && s.Position > 0)
                    throw new ImageProcessingException("The source stream is at the end (have you already read it?). You must call stream.Seek(0, SeekOrigin.Begin); before re-using a stream, or use ImageJob with ResetSourceStream=true the first time the stream is read.");

                if (s != null && s.Length == 0)
                    throw new ImageProcessingException("Source stream is empty; it has a length of 0. No bytes, no data. We can't work with this.");

            } catch (NotSupportedException) {
            }

            return s;
        }


        #region Wrapper overloads
       
        /// <summary>
        /// Resizes and processes the specified source image and stores the encoded result in the specified destination.
        /// If passed a source Stream, Bitmap, or Image instance, it will be disposed after use. Use disposeSource=False to disable that behavior. 
        /// </summary>
        /// <param name="source">May be an instance of string (a physical path or app-relative virtual path), VirtualFile, IVirtualBitmapFile, HttpPostedFile, Bitmap, Image, or Stream. App-relative virtual paths will use the VirtualPathProvider system</param>
        /// <param name="dest">May be a physical path (string), or a Stream instance. Does not have to be seekable.</param>
        /// <param name="settings">Resizing and processing command to apply to the image.</param>
        public virtual void Build(object source, object dest, ResizeSettings settings) {
            Build(source, dest, settings, true);
        }

        /// <summary>
        /// Resizes and processes the specified source image and stores the encoded result in the specified destination.
        /// If passed a source Stream, Bitmap, or Image instance, it will be disposed after use. Use disposeSource=False to disable that behavior. 
        /// </summary>
        /// <param name="source">May be an instance of string (a physical path or app-relative virtual path), VirtualFile, IVirtualBitmapFile, HttpPostedFile, Bitmap, Image, or Stream. App-relative virtual paths will use the VirtualPathProvider system</param>
        /// <param name="dest">May be a physical path (string), or a Stream instance. Does not have to be seekable.</param>
        /// <param name="instructions">Resizing and processing command to apply to the image.</param>
        public virtual ImageJob Build(object source, object dest, Instructions instructions)
        {
            var j = new ImageJob(source, dest, instructions, true, false);
            Build(j);
            return j;
        }

        /// <summary>
        /// Resizes and processes the specified source image and stores the encoded result in the specified destination. 
        /// If passed a source Stream, Bitmap, or Image instance, it will not be disposed unless disposeSource=true.
        /// </summary>
        /// <param name="source">May be an instance of string (a physical path or app-relative virtual path), VirtualFile, IVirtualBitmapFile, HttpPostedFile, Bitmap, Image, or Stream. App-relative virtual paths will use the VirtualPathProvider system</param>
        /// <param name="dest">May be a physical path (string), or a Stream instance. Does not have to be seekable.</param>
        /// <param name="settings">Resizing and processing command to apply to the image.</param>
        /// <param name="disposeSource">True to dispose 'source' after use. False to leave intact.</param>
        public virtual void Build(object source, object dest, ResizeSettings settings, bool disposeSource) {
            Build(source, dest, settings, disposeSource, false);
        }

   

        /// <summary>
        /// Resizes and processes the specified source image and stores the encoded result in the specified destination. 
        /// If passed a source Stream, Bitmap, or Image instance, it will not be disposed unless disposeSource=true.
        /// If passed a path destination, the physical path of the written file will be returned.
        /// </summary>
        /// <param name="source">May be an instance of string (a physical path or app-relative virtual path), VirtualFile, IVirtualBitmapFile, HttpPostedFile, Bitmap, Image, or Stream. App-relative virtual paths will use the VirtualPathProvider system</param>
        /// <param name="dest">May be a physical path (string), or a Stream instance. Does not have to be seekable.</param>
        /// <param name="settings">Resizing and processing command to apply to the image.</param>
        /// <param name="disposeSource">True to dispose 'source' after use. False to leave intact.</param>
        /// <param name="addFileExtension">If true, will add the correct file extension to 'dest' if it is a string. </param>
        [Obsolete("Use .Build(new ImageJob(source, dest, settings, disposeSource, addFileExtension)).FinalPath  instead")]
        public virtual string Build(object source, object dest, ResizeSettings settings, bool disposeSource, bool addFileExtension) {
            return Build(new ImageJob(source, dest, settings, disposeSource, addFileExtension)).FinalPath;
        }
        #endregion

        /// <summary>
        /// The most flexible method for processing an image
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        public virtual ImageJob Build(ImageJob job) {
            if (job == null) throw new ArgumentNullException("job", "ImageJob parameter null. Cannot Build a null ImageJob instance");
            Stopwatch totalTicks = Stopwatch.StartNew();
            //Clone and filter settings FIRST, before calling plugins.
            ResizeSettings s = job.Settings == null ? new ResizeSettings() : new ResizeSettings(job.Settings);
            if (SettingsModifier != null) s = SettingsModifier.Modify(s);
            job.Settings = s;

            try {
                //Allow everything else to be overridden
                if (BuildJob(job) != RequestedAction.Cancel) throw new ImageProcessingException("Nobody did the job");
                EndBuildJob(job);
                totalTicks.Stop();
                job.TotalTicks = totalTicks.ElapsedTicks;
                (SettingsModifier as IPipelineConfig)?.FireHeartbeat();
                Configuration.Performance.GlobalPerf.Singleton.JobComplete(this, job);
                return job;
            } finally {
                //Follow the dispose requests
                if (job.DisposeSourceObject && job.Source is IDisposable && job.Source != null) ((IDisposable)job.Source).Dispose();
                if (job.DisposeDestinationStream && job.Dest is IDisposable && job.Dest != null) ((IDisposable)job.Dest).Dispose();
            }
        }
        
        /// <summary>
        /// Processes an ImageState instance. Used by Build, GetFinalSize, and TranslatePoint. 
        /// Can be overridden by a plugin with the OnProcess method
        /// </summary>
        /// <param name="s"></param>
        //public virtual void Process(ImageState s){
            
        //}

        /// <summary>
        /// Doesn't support flipping. Translate a point on the original bitmap to a point on the new bitmap. If the original point no longer exists, returns Empty
        /// </summary>
        /// <returns></returns>
        public virtual PointF[] TranslatePoints(PointF[] sourcePoints, Size originalSize, ResizeSettings q) {
            ImageState s = new ImageState(q, originalSize, true);
            s.layout.AddInvisiblePolygon("points", sourcePoints);
            Process(s);
            return s.layout["points"];
        }
         


        /// <summary>
        /// Gets the final size of an image
        /// </summary>
        /// <returns></returns>
        public virtual Size GetFinalSize(Size originalSize, ResizeSettings q)
        {
            return GetFinalSize(originalSize, new Instructions(q));
        }

        /// <summary>
        /// Gets the final size of an image
        /// </summary>
        /// <returns></returns>
        public virtual Size GetFinalSize(Size originalSize, Instructions q)
        {
            ImageState s = new ImageState(new ResizeSettings(q), originalSize, true);
            Process(s);
            return s.finalSize;
        }

  

        
        private readonly string[] _supportedFileExtensions = new string[]
             {"bmp","gif","exif","png","tif","tiff","tff","jpg","jpeg", "jpe","jif","jfif","jfi"};

        /// <summary>
        /// Returns a list of the file extensions ImageBuilder can load by default. Plugins can implement IFileExtensionPlugin to add new ones.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<string> GetSupportedFileExtensions() {
            return _supportedFileExtensions;
        }
        
        private readonly string[] _supportedQuerystringKeys = new string[]{
                    "format", "thumbnail", "maxwidth", "maxheight",
                "width", "height","w","h",
                "crop", "page", "bgcolor",
                "rotate", "flip", "sourceFlip", "sFlip", "sRotate", "borderWidth",
                "borderColor", "paddingWidth", "paddingColor",
                 "frame", "useresizingpipeline", 
                "cache", "process", "margin", "dpi", "zoom", "autorotate"};

        /// <summary>
        /// Returns a list of the querystring commands ImageBuilder can parse by default. Plugins can implement IQuerystringPlugin to add new ones.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<string> GetSupportedQuerystringKeys() {
            return _supportedQuerystringKeys;
        }
    }
}
