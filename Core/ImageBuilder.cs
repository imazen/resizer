/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Reflection;
using ImageResizer.Resizing;
using ImageResizer.Encoding;
using ImageResizer.Util;
using ImageResizer.Configuration;
using ImageResizer.Plugins;
using System.Globalization;
using ImageResizer.ExtensionMethods;
using System.Web.Hosting;

namespace ImageResizer
{
    /// <summary>
    /// Provides methods for generating resized images, and for reading and writing them to disk.
    /// Use ImageBuilder.Current to get the current instance (as configured in the application configuration), or use ImageBuilder.Current.Create() to control which extensions are used.
    /// </summary>
    public partial class ImageBuilder : AbstractImageProcessor, IQuerystringPlugin, IFileExtensionPlugin
    {
        /// <summary>
        /// Shouldn't be used except to make a factory instance.
        /// </summary>
        protected ImageBuilder() { }
        protected IEncoderProvider _encoderProvider = null;
        /// <summary>
        /// Handles the encoder selection and provision proccess.
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

        /// <summary>
        /// Create a new instance of ImageBuilder using the specified extensions, encoder provider, file provider, and settings filter. Extension methods will be fired in the order they exist in the collection.
        /// </summary>
        /// <param name="extensions"></param>
        /// <param name="encoderProvider"></param>
        /// <param name="settingsModifier"></param>
        /// <param name="virtualFileProvider"></param>
        public ImageBuilder(IEnumerable<BuilderExtension> extensions, IEncoderProvider encoderProvider, IVirtualImageProvider virtualFileProvider, ISettingsModifier settingsModifier)
            : base(extensions) {
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
        /// Loads a Bitmap from the specified source. If a filename is available, it will be attached to bitmap.Tag in a BitmapTag instance. The Bitmap.Tag.Path value may be a virtual, relative, UNC, windows, or unix path. 
        /// Does not dispose 'source' if it is a Stream or Image instance - that's the responsibility of the calling code.
        /// </summary>
        /// <param name="source">May  be an instance of string, VirtualFile, IVirtualFile IVirtualBitmapFile, HttpPostedFile, Bitmap, Image, or Stream.  If passed an Image instance, the image will be cloned, which will cause metadata, indexed state, and any additional frames to be lost. Accepts physical paths and application relative paths. (C:\... and ~/path) </param>
        /// <param name="settings">Will ignore ICC profile if ?ignoreicc=true.</param>
        /// <returns>A Bitmap. The .Tag property will include a BitmapTag instance. If .Tag.Source is not null, remember to dispose it when you dispose the Bitmap.</returns>
        [Obsolete("This method returns an unmanaged, shoot-yourself-in-the-foot Bitmap instance. Use Build(ImageJob) or LoadImageInfo(source, new string[]{\"source.width\",\"source.height\"})")]
        public virtual Bitmap LoadImage(object source, ResizeSettings settings) {
            return LoadImage(source, settings, false);
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
        /// Loads a Bitmap from the specified source. If a filename is available, it will be attached to bitmap.Tag in a BitmapTag instance. The Bitmap.Tag.Path value may be a virtual, relative, UNC, windows, or unix path. 
        /// Does not dispose 'source' if it is a Stream or Image instance - that's the responsibility of the calling code.
        /// </summary>
        /// <param name="source">May  be an instance of string, VirtualFile, IVirtualFile IVirtualBitmapFile, HttpPostedFile, Bitmap, Image, or Stream.  If passed an Image instance, the image will be cloned, which will cause metadata, indexed state, and any additional frames to be lost. Accepts physical paths and application relative paths. (C:\... and ~/path) </param>
        /// <param name="settings">Will ignore ICC profile if ?ignoreicc=true.</param>
        /// <param name="restoreStreamPos">If true, the position of the source stream will be restored after being read</param>
        /// <returns>A Bitmap. The .Tag property will include a BitmapTag instance. If .Tag.Source is not null, remember to dispose it when you dispose the Bitmap.</returns>
        [Obsolete("This method returns an unmanaged, shoot-yourself-in-the-foot Bitmap instance. Use Build(ImageJob) or LoadImageInfo(source, new string[]{\"source.width\",\"source.height\"})")]
        public virtual Bitmap LoadImage(object source, ResizeSettings settings, bool restoreStreamPos){
            if (source == null) throw new ArgumentNullException("source", "The source argument cannot be null; how do you load an image from a null value?");

            bool disposeStream = !(source is Stream);
            string path = null;

            //Fire PreLoadImage(source,settings)
            this.PreLoadImage(ref source, ref path, ref disposeStream,  ref settings);

            System.Drawing.Bitmap b = null;
            string loadFailureReasons = "File may be corrupted, empty, or may contain a PNG image with a single dimension greater than 65,535 pixels.";
            
            //Bitmap
            if (source is Bitmap) return source as Bitmap;
            //Image
            if (source is System.Drawing.Image) 
                return new Bitmap((System.Drawing.Image)source); //Note, this clones just the raw bitmap data - doesn't copy attributes, bit depth, or anything.
            //IVirtualBitmapFile
            if (source is IVirtualBitmapFile) {
                b = ((IVirtualBitmapFile)source).GetBitmap();
                if (b.Tag == null) b.Tag = new BitmapTag(((IVirtualBitmapFile)source).VirtualPath);
                return b;
            }

            bool restoreStreamPosition = false;
            Stream s = GetStreamFromSource(source, settings, ref disposeStream, out path, out restoreStreamPosition);
            if (s == null) throw new ArgumentException("Source may only be an instance of string, VirtualFile, IVirtualBitmapFile, HttpPostedFile, HttpPostedFileBase, Bitmap, Image, or Stream.", "source");

            if (restoreStreamPos) restoreStreamPosition = true;

            //Save the original stream position if it's an HttpPostedFile
            long originalPosition = (restoreStreamPosition) ? s.Position : - 1;

            try {
                try {
                    //First try DecodeStream
                    b = this.DecodeStream(s, settings, path);
                    //Let the fallbacks work. (Only happens when a plugin overrides DecodeStream and retuns null)
                    if (b == null) throw new ImageCorruptedException("Failed to decode image. Plugin made DecodeStream return null.", null);
                } catch (Exception e) {
                    //if (Debugger.IsAttached) throw e;
                    Debug.Write("Falling back to DecodeStreamFailed: " + e.Message + "\n" + e.StackTrace); 
                    
                    if (!s.CanSeek)
                        throw new ImageCorruptedException("Cannot attempt fallback decoding path on a non-seekable stream", e);

                    b = DecodeStreamFailed(s, settings, path);
                    if (b == null) throw; //If none of the extensions loaded the image, throw the exception anyhow.
                }
            } catch (ArgumentException ae) {
                ae.Data.Add("path", path);
                throw new ImageCorruptedException(loadFailureReasons, ae);
            } catch (ExternalException ee) {
                ee.Data.Add("path", path);
                throw new ImageCorruptedException(loadFailureReasons, ee);
            } finally {
                //Now, we can't dispose the stream if Bitmap is still using it. 
                if (b != null && b.Tag != null && b.Tag is BitmapTag && ((BitmapTag)b.Tag).Source == s) {
                    //And, it looks like Bitmap is still using it.
                    s = null;
                }
                //Dispose the stream if we opened it. If someone passed it to us, they're responsible.
                if (s != null && disposeStream) { s.Dispose(); s = null; }

                //Restore the stream position if we were given an HttpPostedFile instance
                if (originalPosition > -1 && s != null && s.CanSeek) s.Position = originalPosition;

                //Make sure the bitmap is tagged with its path. DecodeStream usually handles this, only relevant for extension decoders.
                if (b != null && b.Tag == null && path != null) b.Tag = new BitmapTag(path);

            }
            PostDecodeStream(ref b,settings);
            return b;            
        }

        /// <summary>
        /// Decodes the stream into a System.Drawing.Bitmap instance. As of 3.0.7, now ensures the stream can safely be closed after the method returns.
        /// May copy the stream. The copied stream will be in b.Tag.Source. Does not close or dispose any streams.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="settings"></param>
        /// <param name="optionalPath"></param>
        /// <returns></returns>
        public override Bitmap DecodeStream(Stream s, ResizeSettings settings, string optionalPath) {
            Bitmap b = base.DecodeStream(s, settings, optionalPath);
            if (b != null && b.Tag == null) b.Tag = new BitmapTag(optionalPath); //Assume Bitmap wasn't used.
            if (b != null) return b;

            bool useICM = true;
            if (settings != null && "true".Equals(settings["ignoreicc"], StringComparison.OrdinalIgnoreCase)) useICM = false;

            //NDJ - May 24, 2011 - Copying stream into memory so the original can be closed safely.
            MemoryStream ms = s.CopyToMemoryStream(); 
            b = new Bitmap(ms, useICM); 
            b.Tag = new BitmapTag(optionalPath, ms); //May 25, 2011: Storing a ref to the MemorySteam so it won't accidentally be garbage collected.
            return b;
        }
        /// <summary>
        /// For plugin use only. 
        /// Returns a stream instance from the specified source object and settings object. 
        /// To exend this method, override GetStream.
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
        /// Resizes and processes the specified source image and returns a bitmap of the result.
        /// Note! 
        /// This method assumes that transparency will be supported in the final output format, and therefore does not apply a matte color. Use &amp;bgcolor to specify a background color
        /// if you use this method with a non-transparent format such as Jpeg.
        /// If passed a source Stream, Bitmap, or Image instance, it will be disposed after use. Use disposeSource=False to disable that behavior. 
        /// </summary>
        /// <param name="source">May be an instance of string (a physical path), VirtualFile, IVirtualBitmapFile, HttpPostedFile, Bitmap, Image, or Stream.</param>
        /// <param name="settings">Resizing and processing command to apply to the.</param>
        [Obsolete("Use ImageJob with dest=typeof(Bitmap) instead - but only as a last resort. This method returns an umanaged, un-garbage collected object that can kill your server. Use Build(source,dest, instructions) instead of handing the Bitmap instance yourself.")]
        public virtual Bitmap Build(object source, ResizeSettings settings) {
            return Build(source, settings, true);
        }
        /// <summary>
        /// Resizes and processes the specified source image and returns a bitmap of the result.
        /// Note! 
        /// This method assumes that transparency will be supported in the final output format, and therefore does not apply a matte color. Use &amp;bgcolor to specify a background color
        /// if you use this method with a non-transparent format such as Jpeg.
        /// 
        /// If passed a source Stream, Bitmap, or Image instance, it will not be disposed unless disposeSource=true.
        /// </summary>
        /// <param name="source">May be an instance of string (a physical path), VirtualFile, IVirtualBitmapFile, HttpPostedFile, Bitmap, Image, or Stream.</param>
        /// <param name="settings">Resizing and processing command to apply to the.</param>
        /// <param name="disposeSource">If false, 'source' will not be disposed. </param>
        [Obsolete("Use ImageJob with dest=typeof(Bitmap) instead - but only as a last resort. This method returns an umanaged, un-garbage collected object that can kill your server. Use Build(source,dest) instead of handing the Bitmap instance yourself.")]
        public virtual Bitmap Build(object source, ResizeSettings settings, bool disposeSource) {
            ImageJob j = new ImageJob(source, typeof(Bitmap), settings, disposeSource, false);
            Build(j);
            return j.Result as Bitmap;
        }

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

            //Clone and filter settings FIRST, before calling plugins.
            ResizeSettings s = job.Settings == null ? new ResizeSettings() : new ResizeSettings(job.Settings);
            if (SettingsModifier != null) s = SettingsModifier.Modify(s);
            job.Settings = s;

            try {
                //Allow everything else to be overriden
                if (BuildJob(job) != RequestedAction.Cancel) throw new ImageProcessingException("Nobody did the job");
                EndBuildJob(job);
                return job;
            } finally {
                //Follow the dispose requests
                if (job.DisposeSourceObject && job.Source is IDisposable && job.Source != null) ((IDisposable)job.Source).Dispose();
                if (job.DisposeDestinationStream && job.Dest is IDisposable && job.Dest != null) ((IDisposable)job.Dest).Dispose();
            }
        }



        protected override RequestedAction BuildJob(ImageJob job) {
            if (base.BuildJob(job) == RequestedAction.Cancel) return RequestedAction.Cancel;

            Bitmap b = null;
            try {
                ResizeSettings s = job.Settings;

                //Load image
                b = LoadImage(job.Source, s, job.ResetSourceStream);

                //Save source path info
                job.SourcePathData = (b != null && b.Tag != null && b.Tag is BitmapTag) ? ((BitmapTag)b.Tag).Path : job.SourcePathData;

                job.ResultInfo["source.width"] = b.Width;
                job.ResultInfo["source.height"] = b.Height;

                //Calucalte the appropriate file extension and mime type
                if (job.Dest != typeof(Bitmap)){
                    IEncoder e = this.EncoderProvider.GetEncoder(s, b);
                    if (e != null)
                    {
                        job.ResultInfo["result.ext"] = e.Extension;
                        job.ResultInfo["result.mime"] = e.MimeType;
                    }
                }

                if (job.Dest == typeof(IDictionary<string, object>))
                {
                    //They only want information/attributes
                    job.Result = job.ResultInfo;
                    return RequestedAction.Cancel;
                }

                //Fire PreAcquireStream(ref dest, settings) to modify 'dest'
                object dest = job.Dest;
                this.PreAcquireStream(ref dest, s);
                job.Dest = dest;

                if (dest == typeof(Bitmap))
                {
                    job.Result = BuildJobBitmapToBitmap(job, b, true);
                    
                    //Write to Physical file
                } else if (dest is string) {
                    //Make physical and resolve variable references all at the same time.
                    job.FinalPath = job.ResolveTemplatedPath(dest as string,
                        delegate(string var) {
                            if ("ext".Equals(var, StringComparison.OrdinalIgnoreCase)) {
                                return job.ResultFileExtension;
                            }
                            if ("width".Equals(var, StringComparison.OrdinalIgnoreCase))
                                return GetFinalSize(new System.Drawing.Size(b.Width, b.Height), new ResizeSettings(job.Settings)).Width.ToString(NumberFormatInfo.InvariantInfo);
                            if ("height".Equals(var, StringComparison.OrdinalIgnoreCase))
                                return GetFinalSize(new System.Drawing.Size(b.Width, b.Height), new ResizeSettings(job.Settings)).Height.ToString(NumberFormatInfo.InvariantInfo);
                            return null;
                        });
                    //If requested, auto-create the parent directory(ies)
                    if (job.CreateParentDirectory) {
                        string dirName = Path.GetDirectoryName(job.FinalPath);
                        if (!Directory.Exists(dirName)) Directory.CreateDirectory(dirName);
                    }
                    bool finishedWrite = false;
                    try{
                        System.IO.FileStream fs = new FileStream(job.FinalPath, FileMode.Create, FileAccess.Write);
                        using (fs) {
                            BuildJobBitmapToStream(job, b, fs);
                            fs.Flush(true);
                            finishedWrite = true;
                        }
                    } finally {
                        //Don't leave half-written files around.
                        if (!finishedWrite) try { if (File.Exists(job.FinalPath)) File.Delete(job.FinalPath); }
                            catch { }
                    }

                    //Write to Unknown stream
                } else if (dest is Stream) {
                    BuildJobBitmapToStream(job, b, (Stream)dest);
                } else throw new ArgumentException("Destination may be a string or Stream.", "Dest");

            } finally {
                //Get the source bitmap's underlying stream (may differ from 'source')
                Stream underlyingStream = null;
                if (b != null && b.Tag != null && b.Tag is BitmapTag) underlyingStream = ((BitmapTag)b.Tag).Source;

                //Close the source bitamp's underlying stream unless it is the same stream (EDIT: or bitmap) we were passed.
                var closeUnderlyingStream = (b != job.Source && underlyingStream != job.Source && underlyingStream != null);

                //Dispose the bitmap unless we were passed it. We check for 'null' in case an ImageCorruptedException occured. 
                if (b != null && b != job.Source) b.Dispose();

                //Dispose the underlying stream after disposing the bitmap
                if (closeUnderlyingStream) underlyingStream.Dispose();
            }

            return RequestedAction.Cancel;
        }

        /// <summary>
        /// Override this when you need to override the behavior of image encoding and/or Bitmap processing
        /// Not for external use. Does NOT dispose of 'source' or 'source's underlying stream.
        /// </summary>
        /// <param name="job"></param>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        protected override RequestedAction BuildJobBitmapToStream(ImageJob job, Bitmap source, Stream dest) {
            if (base.BuildJobBitmapToStream(job,source, dest) == RequestedAction.Cancel) return RequestedAction.None;

            IEncoder e = this.EncoderProvider.GetEncoder(job.Settings,source);
            if (e == null) throw new ImageProcessingException("No image encoder was found for this request.");
            using (Bitmap b = BuildJobBitmapToBitmap(job, source, e.SupportsTransparency))
            {//Determines output format, includes code for saving in a variety of formats.
                //Save to stream
                BeforeEncode(job);
                e.Write(b, dest);
            }
            return RequestedAction.None;
        }

        /// <summary>
        /// Override this when you need to override the behavior of Bitmap processing. 
        /// Not for external use. Does NOT dispose of 'source' or 'source's underlying stream.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="job"></param>
        /// <param name="transparencySupported">True if the output method will support transparency. If false, the image should be provided a matte color</param>
        /// <returns></returns>
        protected Bitmap BuildJobBitmapToBitmap(ImageJob job, Bitmap source, bool transparencySupported) {
            Bitmap b = null;
            using (ImageState state = new ImageState(job.Settings, source.Size, transparencySupported)) {
                state.sourceBitmap = source;
                state.Job = job;

                //Generic processing of ImageState instances.
                Process(state);

                //Save a reference to return
                b = state.destBitmap;
                state.destBitmap = null; //So it doesn't get disposed yet

                //Don't dispose the source bitmap either, just the graphics object.
                state.sourceBitmap = null;
            }
            return b;
        }

        /// <summary>
        /// Processes an ImageState instance. Used by Build, GetFinalSize, and TranslatePoint. 
        /// Can be overriden by a plugin with the OnProcess method
        /// </summary>
        /// <param name="s"></param>
        public virtual void Process(ImageState s){
            if (OnProcess(s) == RequestedAction.Cancel) return;
            PrepareSourceBitmap(s);  // We select the page/frame and flip the source bitmap here
            PostPrepareSourceBitmap(s);
            Layout(s); //Layout everything
            PrepareDestinationBitmap(s); //Create a bitmap and graphics object based on s.destSize
            Render(s); //Render using the graphics object
            ProcessFinalBitmap(s); //Perform the final flipping of the bitmap.
            EndProcess(s);
        }
        /// <summary>
        /// Process.3: Handles the layout phase of Processing
        /// </summary>
        /// <param name="s"></param>
        protected override RequestedAction Layout(ImageState s) {
            if (base.Layout(s) == RequestedAction.Cancel) return RequestedAction.Cancel;
            FlipExistingPoints(s); //Not implemented
            LayoutImage(s);
            PostLayoutImage(s);
            LayoutPadding(s);
            PostLayoutPadding(s);
            LayoutBorder(s);
            PostLayoutBorder(s);
            LayoutEffects(s);
            PostLayoutEffects(s);
            LayoutMargin(s);
            PostLayoutMargin(s);
            LayoutRotate(s);
            PostLayoutRotate(s);
            LayoutNormalize(s);
            PostLayoutNormalize(s);
            LayoutRound(s);
            PostLayoutRound(s);
            EndLayout(s);
            return RequestedAction.None;
        }
        /// <summary>
        /// Handles the rendering phase of processing
        /// </summary>
        /// <param name="s"></param>
        protected override RequestedAction Render(ImageState s) {
            if (base.Render(s) == RequestedAction.Cancel) return RequestedAction.Cancel;
            RenderBackground(s);
            PostRenderBackground(s);
            RenderEffects(s);
            PostRenderEffects(s);
            RenderPadding(s);
            PostRenderPadding(s);
            CreateImageAttribues(s);
            PostCreateImageAttributes(s);
            PreRenderImage(s);
            RenderImage(s);
            PostRenderImage(s);
            RenderBorder(s);
            PostRenderBorder(s);
            PreRenderOverlays(s);
            RenderOverlays(s);
            PreFlushChanges(s);
            FlushChanges(s);
            PostFlushChanges(s);
            return RequestedAction.None;
        }

        /// <summary>
        /// Process.1 Switches the bitmap to the correct frame or page, and applies source flipping commands
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        protected override RequestedAction PrepareSourceBitmap(ImageState s)
        {
            if (base.PrepareSourceBitmap(s) == RequestedAction.Cancel) return RequestedAction.Cancel ; //Call extensions

            if (s.sourceBitmap == null) return RequestedAction.None ; //Nothing to do if there is no bitmap

            Bitmap src = s.sourceBitmap;
            ResizeSettings q = s.settings;

            int page = 0;
            if (!string.IsNullOrEmpty(q["page"]) && !int.TryParse(q["page"], NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out page))
                page = 0;

            int frame = 0;
            if (!string.IsNullOrEmpty(q["frame"]) && !int.TryParse(q["frame"], NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out frame))
                frame = 0;

            //So users can use 1-based numbers
            page--; frame--;

            //Support page selection in a .tiff document.
            try {
                //Stay on the last page/frame if out of bounds
                if (page > 0 && page >= src.GetFrameCount(FrameDimension.Page)) page = src.GetFrameCount(FrameDimension.Page) - 1;
                if (frame > 0 && frame >= src.GetFrameCount(FrameDimension.Time)) frame = src.GetFrameCount(FrameDimension.Time) - 1;

                //Select the right page/frame if specified
                if (page > 0) src.SelectActiveFrame(FrameDimension.Page, page);
                if (frame > 0) src.SelectActiveFrame(FrameDimension.Time, frame);
                s.originalSize = s.sourceBitmap.Size;
            } catch (ExternalException) { } //When somebody tries &frame or &page on a single-frame image
            


            //Flipping has to be done on the original - it can't be done as part of the DrawImage or later, after the borders are drawn.

            if (s.sourceBitmap != null && (s.settings.SourceFlip != RotateFlipType.RotateNoneFlipNone || !string.IsNullOrEmpty(s.settings["sRotate"]))) {
                double angle = s.settings.Get<double>("sRotate",0);

                s.EnsurePreRenderBitmap();
                s.preRenderBitmap.RotateFlip(PolygonMath.CombineFlipAndRotate(s.settings.SourceFlip, angle));
                s.originalSize = s.preRenderBitmap.Size;
            }
            return RequestedAction.None;
        }

       

  

        protected override RequestedAction LayoutPadding(ImageState s) {
            if (base.LayoutPadding(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions

            //We need to add padding
            if (!s.settings.Padding.IsEmpty) {
                s.layout.AddRing("padding",  s.settings.Padding);
            }
            return RequestedAction.None;
        }

        protected override RequestedAction LayoutMargin(ImageState s) {
            if (base.LayoutMargin(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions

            //We need to add padding
            if (!s.settings.Margin.IsEmpty) {
                s.layout.AddRing("margin", s.settings.Margin);
            }
            return RequestedAction.None;
        }

        protected override RequestedAction LayoutBorder(ImageState s) {
            if (base.LayoutBorder(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions

            //And borders
            if (!s.settings.Border.IsEmpty) {
                s.layout.AddRing("border", s.settings.Border);
            }
            return RequestedAction.None;
        }

        

        protected override RequestedAction LayoutRound(ImageState s)
        {
            if (base.LayoutRound(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions
            //Todo, round points here.
            //s.layout.Round();
            return RequestedAction.None;
        }

        protected override RequestedAction LayoutRotate(ImageState s)
        {
            if (base.LayoutRotate(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions
            //Now, rotate all rings.
            s.layout.Rotate(s.settings.Rotate, new PointF(0, 0));
            return RequestedAction.None;
        }
        protected override RequestedAction LayoutNormalize(ImageState s)
        {
            if (base.LayoutNormalize(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions
            //Normalize all the rings
            s.layout.Normalize(new PointF(0, 0));
            return RequestedAction.None;
        }


        protected override RequestedAction EndLayout(ImageState s)
        {
            if (base.EndLayout(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions

            //Calculates a bounding box around all the rings in the layout, then rounds that size. Creates a 1x1 pixel destSize value at minimum.
            s.destSize = PolygonMath.RoundPoints(s.layout.GetBoundingBox().Size);
            s.destSize = new Size((int)Math.Max(1, s.destSize.Width), (int)Math.Max(1, s.destSize.Height));
            return RequestedAction.None;
        }

        /// <summary>
        /// Creates a bitmap of s.destSize dimensions, intializes a graphics object for it, and configures all the default settings.
        /// </summary>
        /// <param name="s"></param>
        protected override RequestedAction  PrepareDestinationBitmap(ImageState s)
        {
            if (base.PrepareDestinationBitmap(s) == RequestedAction.Cancel) return RequestedAction.Cancel;

             if (s.sourceBitmap == null) return RequestedAction.None;

            //Create new bitmap using calculated size. 
            s.destBitmap = new Bitmap(s.destSize.Width,s.destSize.Height, PixelFormat.Format32bppArgb);
            

            //Create graphics handle
            Graphics g = s.destGraphics = Graphics.FromImage(s.destBitmap);

            //High quality everthing
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            g.CompositingMode = CompositingMode.SourceOver;
            return RequestedAction.None;
        }

        /// <summary>
        /// Sets the background color if needed or requested
        /// </summary>
        /// <param name="s"></param>
        protected override RequestedAction RenderBackground(ImageState s) {
            if (base.RenderBackground(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions

            //Skip this when we are doing simulations
            if (s.destGraphics == null) return RequestedAction.None;

            Graphics g = s.destGraphics;

            //If the image doesn't support transparency, we need to fill the background color now.
            Color background = s.settings.BackgroundColor;

 

            //Find out if we can safely know that nothing will be showing from behind the image (no margin, padding, etc, and source format doesn't have alpha channel).
            //Doesn't know anything about s.preRenderBitmap
            bool nothingToShow = (s.sourceBitmap != null && (s.sourceBitmap.PixelFormat == PixelFormat.Format24bppRgb ||
                        s.sourceBitmap.PixelFormat == PixelFormat.Format32bppRgb || 
                        s.sourceBitmap.PixelFormat == PixelFormat.Format48bppRgb) &&
                        PolygonMath.ArraysEqual(s.layout["image"], s.layout.LastRing.points) &&
                        PolygonMath.IsUnrotated(s.layout["image"]) && string.IsNullOrEmpty(s.settings["s.alpha"])
                            && string.IsNullOrEmpty(s.settings["s.roundcorners"])
                            && string.IsNullOrEmpty(s.settings["filter"]));

            //Set the background to white if the background will be showing and the destination format doesn't support transparency.
            if (background == Color.Transparent && !s.supportsTransparency & !nothingToShow) 
                background = Color.White;


            
            //Fill background
            if (background != Color.Transparent) //This causes increased aliasing at the edges - i.e., a faint white border that is even more pronounced than usual.
                g.Clear(background); //Does this work for Color.Transparent? -- 
            return RequestedAction.None;
        }


        protected override RequestedAction RenderPadding(ImageState s) {
            if (base.RenderPadding(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions

            //Skip this when we are doing simulations
            if (s.destGraphics == null) return RequestedAction.None;

            if (!s.layout.ContainsRing("padding")) return RequestedAction.None;

            Color paddingColor = s.settings.PaddingColor;
            //Inherit color
            if (paddingColor.Equals(Color.Transparent)) paddingColor = s.settings.BackgroundColor;
            //Draw padding around image if needed.
            if (!paddingColor.Equals(s.settings.BackgroundColor) && paddingColor != Color.Transparent)
                using (Brush b = new SolidBrush(paddingColor)) s.destGraphics.FillPolygon(b, s.layout["padding"]);
          
            return RequestedAction.None;
        }

        protected override RequestedAction CreateImageAttribues(ImageState s) {
            if (base.CreateImageAttribues(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions
            return RequestedAction.None;
        }

        protected override RequestedAction RenderImage(ImageState s) {
            if (base.RenderImage(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions

            //Skip this when we are doing simulations
            if (s.destGraphics == null) return RequestedAction.None;

            if (!string.IsNullOrEmpty(s.settings["gdi.filter"])) {
                s.destGraphics.InterpolationMode = s.settings.Get<InterpolationMode>("gdi.filter", s.destGraphics.InterpolationMode);
            }

            if (s.preRenderBitmap != null) {
                using (Bitmap b = s.preRenderBitmap) { //Dispose the intermediate bitmap aggressively
                    this.InternalGraphicsDrawImage(s, s.destBitmap, s.preRenderBitmap, PolygonMath.getParallelogram(s.layout["image"]), 
                        s.copyRect, s.colorMatrix);
                }
            } else { 
                this.InternalGraphicsDrawImage(s,s.destBitmap,s.sourceBitmap, PolygonMath.getParallelogram(s.layout["image"]), s.copyRect,  s.colorMatrix);
            }
            return RequestedAction.None;
        }

        protected override RequestedAction InternalGraphicsDrawImage(ImageState state, Bitmap dest, Bitmap source, PointF[] targetArea, RectangleF sourceArea, float[][] colorMatrix)
        {
            if (base.InternalGraphicsDrawImage(state, dest, source, targetArea, sourceArea, colorMatrix) == RequestedAction.Cancel) return RequestedAction.Cancel;
            using (var ia = new ImageAttributes())
            {
                ia.SetWrapMode(WrapMode.TileFlipXY);
            
                if (colorMatrix != null) ia.SetColorMatrix(new ColorMatrix(colorMatrix));
                state.destGraphics.DrawImage(source, targetArea, sourceArea, GraphicsUnit.Pixel, ia);
            
            }
            return RequestedAction.Cancel;
        }
        protected override RequestedAction RenderBorder(ImageState s) {
            if (base.RenderBorder(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions

            //Skip this when we are doing simulations
            if (s.destGraphics == null) return RequestedAction.None;

            //Draw border
            if (s.settings.Border.IsEmpty) return RequestedAction.None;

            if (double.IsNaN(s.settings.Border.All)) {
                float[] widths = new float[] { (float)s.settings.Border.Top * -1, (float)s.settings.Border.Right * -1, (float)s.settings.Border.Bottom * -1, (float)s.settings.Border.Left * -1 };
                PointF[,] corners = PolygonMath.GetCorners(s.layout["border"],widths);

                for (int i = 0; i <= corners.GetUpperBound(0); i++) {
                    int last = i == 0 ? corners.GetUpperBound(0) : i -1;

                    PointF start = PolygonMath.Average(corners[last, 3], corners[last, 0]);
                    PointF end = PolygonMath.Average(corners[i, 0], corners[i, 1]);

                    using (Pen p = new Pen(s.settings.BorderColor, widths[i < 1 ? 3 : i -1] * -1)) {
                        p.Alignment = System.Drawing.Drawing2D.PenAlignment.Center; //PenAlignment.Center is the only supported mode.
                        p.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
                        s.destGraphics.DrawLine(p, start, end);
                    }

                }
            } else {
                using (Pen p = new Pen(s.settings.BorderColor, (float)s.settings.Border.All)) {
                    p.Alignment = System.Drawing.Drawing2D.PenAlignment.Center; //PenAlignment.Center is the only supported mode.
                    p.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
                    s.destGraphics.DrawPolygon(p, PolygonMath.InflatePoly(s.layout["border"], (float)(s.settings.Border.All / -2.0))); //I hope GDI rounds the same way as .NET.. Otherwise there may be an off-by-one error..
                }
            }

            return RequestedAction.None;
        }


        protected override RequestedAction FlushChanges(ImageState s) {
            if (base.FlushChanges(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions

            if (s.destGraphics == null) return RequestedAction.None;
            //Commit changes.
            s.destGraphics.Flush(FlushIntention.Flush);
            s.destGraphics.Dispose();
            s.destGraphics = null;
            return RequestedAction.None;
        }

        protected override RequestedAction ProcessFinalBitmap(ImageState s) {
            if (base.ProcessFinalBitmap(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions

            //The default if we are doing a simulation

            s.finalSize = s.destSize;
            //Skip this when we are doing simulations
            if (s.destBitmap == null) return RequestedAction.None;

            //The last flipping.
            if (s.settings.Flip != RotateFlipType.RotateNoneFlipNone)
                s.destBitmap.RotateFlip(s.settings.Flip);

            //Set DPI value
            if (!string.IsNullOrEmpty(s.settings["dpi"])){
                int dpi = s.settings.Get<int>("dpi",96);
                s.destBitmap.SetResolution(dpi, dpi);
            }
            
            s.finalSize = s.destBitmap.Size;
            return RequestedAction.None;
        }
       

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

  




        /// <summary>
        /// Populates copyRect, as well as Rings image and imageArea. Translates and scales any existing rings as if they existed on the original bitmap.
        /// </summary>
        /// <param name="s"></param>
        protected override RequestedAction LayoutImage(ImageState s) {
            if (base.LayoutImage(s) == RequestedAction.Cancel) return RequestedAction.Cancel; //Call extensions

            if (s.copyRect.IsEmpty) {
                //Use the crop size if present.
                s.copyRect = new RectangleF(new PointF(0, 0), s.originalSize);
                if (s.settings.GetList<double>("crop", 0, 4) != null) {
                    s.copyRect = PolygonMath.ToRectangle(s.settings.getCustomCropSourceRect(s.originalSize)); //Round the custom crop rectangle coordinates
                    if (s.copyRect.Size.IsEmpty) throw new Exception("You must specify a custom crop rectange if crop=custom");
                }
            }
            //Save the manual crop size.
            SizeF manualCropSize = s.copySize;
            RectangleF manualCropRect = s.copyRect;

            FitMode fit = s.settings.Mode;
            //Determine fit mode to use if both vertical and horizontal limits are used.
            if (fit == FitMode.None){
                if (s.settings.Width != -1 || s.settings.Height != -1){

                    if ("fill".Equals(s.settings["stretch"], StringComparison.OrdinalIgnoreCase)) fit = FitMode.Stretch;
                    else if ("auto".Equals(s.settings["crop"], StringComparison.OrdinalIgnoreCase)) fit = FitMode.Crop;
                    else if (!string.IsNullOrEmpty(s.settings["carve"]) 
                        && !"false".Equals(s.settings["carve"], StringComparison.OrdinalIgnoreCase)
                        && !"none".Equals(s.settings["carve"], StringComparison.OrdinalIgnoreCase)) fit = FitMode.Carve;
                    else fit = FitMode.Pad;
                }else{
                    fit = FitMode.Max;
                }

            }
            



            //Aspect ratio of the image
            double imageRatio = s.copySize.Width / s.copySize.Height;

            //Zoom factor
            double zoom = s.settings.Get<double>("zoom", 1);

            //The target size for the image 
            SizeF targetSize = new SizeF(-1, -1);
            //Target area for the image
            SizeF areaSize = new SizeF(-1, -1);
            //If any dimensions are specified, calculate. Otherwise, use original image dimensions
            if (s.settings.Width != -1 || s.settings.Height != -1 || s.settings.MaxHeight != -1 || s.settings.MaxWidth != -1) {
                //A dimension was specified. 
                //We first calculate the largest size the image can be under the width/height/maxwidth/maxheight restrictions.
                //- pretending stretch=fill and scale=both

                //Temp vars - results stored in targetSize and areaSize
                double width = s.settings.Width;
                double height = s.settings.Height;
                double maxwidth = s.settings.MaxWidth;
                double maxheight = s.settings.MaxHeight;

                //Eliminate cases where both a value and a max value are specified: use the smaller value for the width/height 
                if (maxwidth > 0 && width > 0) {   width = Math.Min(maxwidth, width);    maxwidth = -1;  }
                if (maxheight > 0 && height > 0) { height = Math.Min(maxheight, height); maxheight = -1; }

                //Handle cases of width/maxheight and height/maxwidth as in legacy versions. 
                if (width != -1 && maxheight != -1) maxheight = Math.Min(maxheight, (width / imageRatio));
                if (height != -1 && maxwidth != -1) maxwidth = Math.Min(maxwidth, (height * imageRatio));


                //Move max values to width/height. FitMode should already reflect the mode we are using, and we've already resolved mixed modes above.
                width = Math.Max(width, maxwidth);
                height = Math.Max(height, maxheight);

                //Calculate missing value (a missing value is handled the same everywhere). 
                if (width > 0 && height <= 0) height = width/ imageRatio;
                else if (height > 0 && width <= 0) width = height * imageRatio;

                //We now have width & height, our target size. It will only be a different aspect ratio from the image if both 'width' and 'height' are specified.

                //FitMode.Max
                if (fit == FitMode.Max) {
                    areaSize = targetSize = PolygonMath.ScaleInside(manualCropSize, new SizeF((float)width, (float)height));
                //FitMode.Pad
                } else if (fit == FitMode.Pad) {
                    areaSize = new SizeF((float)width, (float)height);
                    targetSize = PolygonMath.ScaleInside(manualCropSize, areaSize);
                //FitMode.crop
                } else if (fit == FitMode.Crop) {
                    //We autocrop - so both target and area match the requested size
                    areaSize = targetSize = new SizeF((float)width, (float)height);
                    RectangleF copyRect;
                    
                    ScaleMode scale = s.settings.Scale;
                    bool cropWidthSmaller = manualCropSize.Width <= (float)width;
                    bool cropHeightSmaller = manualCropSize.Height <= (float)height;

                    // With both DownscaleOnly (where only one dimension is smaller than
                    // requested) and UpscaleCanvas, we will have a targetSize based on the
                    // minWidth & minHeight.
                    // TODO: what happens if mode=crop;scale=down but the target is larger than the source?
                    if ((scale == ScaleMode.DownscaleOnly && (cropWidthSmaller != cropHeightSmaller)) ||
                        (scale == ScaleMode.UpscaleCanvas))
                    {
                        var minWidth = Math.Min(manualCropSize.Width, (float)width);
                        var minHeight = Math.Min(manualCropSize.Height, (float)height);

                        targetSize = new SizeF(minWidth, minHeight);
                        copyRect = manualCropRect = new RectangleF(0, 0, minWidth, minHeight);

                        // For DownscaleOnly, the areaSize is adjusted to the new targetSize as well.
                        if (scale == ScaleMode.DownscaleOnly)
                        {
                            areaSize = targetSize;
                        }
                    }
                    else
                    {
                        //Determine the size of the area we are copying
                        Size sourceSize = PolygonMath.RoundPoints(PolygonMath.ScaleInside(areaSize, manualCropSize));
                        //Center the portion we are copying within the manualCropSize
                        copyRect = new RectangleF(0, 0, sourceSize.Width, sourceSize.Height);
                    }

                    // Align the actual source-copy rectangle inside the available
                    // space based on the anchor.
                    s.copyRect = PolygonMath.ToRectangle(PolygonMath.AlignWith(copyRect, s.copyRect, s.settings.Anchor));

                } else { //Stretch and carve both act like stretching, so do that:
                    areaSize = targetSize = new SizeF((float)width, (float)height);
                }


            }else{
                //No dimensions specified, no fit mode needed. Use manual crop dimensions
                areaSize = targetSize = manualCropSize; 
            }

            //Multiply both areaSize and targetSize by zoom. 
            areaSize.Width *= (float)zoom;
            areaSize.Height *= (float)zoom;
            targetSize.Width *= (float)zoom;
            targetSize.Height *= (float)zoom;
            
            //Todo: automatic crop is permitted to break the scaling rules. Fix!!

            //Now do upscale/downscale checks. If they take effect, set targetSize to imageSize
            if (s.settings.Scale == ScaleMode.DownscaleOnly) {
                if (PolygonMath.FitsInside(manualCropSize, targetSize)) {
                    //The image is smaller or equal to its target polygon. Use original image coordinates instead.
                    areaSize = targetSize = manualCropSize;
                    s.copyRect = manualCropRect;
                }
            } else if (s.settings.Scale == ScaleMode.UpscaleOnly) {
                if (!PolygonMath.FitsInside(manualCropSize, targetSize)) {
                    //The image is larger than its target. Use original image coordintes instead
                    areaSize = targetSize = manualCropSize;
                    s.copyRect = manualCropRect;
                }
            } else if (s.settings.Scale == ScaleMode.UpscaleCanvas) {
                //Same as downscaleonly, except areaSize isn't changed.
                if (PolygonMath.FitsInside(manualCropSize, targetSize)) {
                    //The image is smaller or equal to its target polygon. 
                    
                    //Use manual copy rect/size instead.

                    targetSize = manualCropSize;
                    s.copyRect = manualCropRect;
                }
            }


            //May 12: require max dimension and round values to minimize rounding differences later.
            areaSize.Width = Math.Max(1, (float)Math.Round(areaSize.Width));
            areaSize.Height = Math.Max(1, (float)Math.Round(areaSize.Height));
            targetSize.Width = Math.Max(1, (float)Math.Round(targetSize.Width));
            targetSize.Height = Math.Max(1, (float)Math.Round(targetSize.Height));
            

            //Translate and scale all existing rings
            s.layout.Shift(s.copyRect, new RectangleF(new Point(0, 0), targetSize));

            s.layout.AddRing("image", PolygonMath.ToPoly(new RectangleF(new PointF(0, 0), targetSize)));

            s.layout.AddRing("imageArea",PolygonMath.ToPoly(new RectangleF(new PointF(0, 0), areaSize)));

            //Center imageArea around 'image'
            s.layout["imageArea"] = PolygonMath.AlignWith(s.layout["imageArea"], s.layout["image"], s.settings.Anchor);

            return RequestedAction.None;
        }

        protected override RequestedAction EndProcess(ImageState s)
        {
            if (base.EndProcess(s) == RequestedAction.Cancel) return RequestedAction.Cancel;
            if (s.Job != null)
            {
                //Save the final dimensions.
                s.Job.ResultInfo["final.width"] = s.finalSize.Width;
                s.Job.ResultInfo["final.height"] = s.finalSize.Height;
            }
            return RequestedAction.None;
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
                "scale", "stretch", "crop", "cropxunits", "cropyunits", "page", "bgcolor",
                "rotate", "flip", "sourceFlip", "sFlip", "sRotate", "borderWidth",
                "borderColor", "paddingWidth", "paddingColor",
                "ignoreicc", "frame", "useresizingpipeline", 
                "cache", "process", "margin", "anchor","dpi","mode", "zoom"};

        /// <summary>
        /// Returns a list of the querystring commands ImageBuilder can parse by default. Plugins can implement IQuerystringPlugin to add new ones.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<string> GetSupportedQuerystringKeys() {
            return _supportedQuerystringKeys;
        }
    }
}
