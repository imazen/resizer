using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ImageResizer.Util;

namespace ImageResizer {

    public class ImageJob {
        public ImageJob() {
        }
        public ImageJob(string sourcePath, string destPath, ResizeSettings settings) {
            this.Source = sourcePath;
            this.Dest = destPath;
            this.Settings = settings;
        }

        public ImageJob(Stream sourceStream, Stream destStream, ResizeSettings settings) {
            this.Source = sourceStream;
            this.Dest = destStream;
            this.Settings = settings;
        }

        public ImageJob(object source, object dest, ResizeSettings settings) {
            this.Source = source;
            this.Dest = dest;
            this.Settings = settings;
        }

        public ImageJob(object source, object dest, ResizeSettings settings, bool disposeSource, bool addFileExtension) {
            this.Source = source;
            this.Dest = dest;
            this.Settings = settings;
            this.DisposeSourceObject = disposeSource;
            this.AddFileExtension = addFileExtension;
        }

        /// <summary>
        /// Shorthand method for ImageBuilder.Current.Build(this)
        /// </summary>
        /// <returns></returns>
        public ImageJob Build() {
            return ImageBuilder.Current.Build(this);
        }
        private object _source = null;
        /// <summary>
        /// The source image's physical path, app-relative virtual path, or a Stream, byte array, Bitmap, VirtualFile, IVirtualFile, HttpPostedFile, or HttpPostedFileBase instance.
        /// </summary>
        public object Source {
            get { return _source; }
            set { _source = value; }
        }

        private object _dest = null;
        /// <summary>
        /// The destination Stream, physical path, or app-relative virtual path. If a Bitmap instance is desired, 
        /// set this to typeof(System.Drawing.Bitmap). The result will be stored in .Result
        /// </summary>
        public object Dest {
            get { return _dest; }
            set { _dest = value; }
        }

        private object _result = null;
        /// <summary>
        /// The result if a Bitmap, BitmapSource, or IWICBitmapSource instance is requested. 
        /// </summary>
        public object Result {
            get { return _result; }
            set { _result = value; }
        }

        private ResizeSettings _settings = null;
        /// <summary>
        /// The image processing settings
        /// </summary>
        public ResizeSettings Settings {
            get { return _settings; }
            set { _settings = value; }
        }

        private bool _disposeSourceObject = true;
        /// <summary>
        /// If true, and if 'source' is a IDisposable instead like Bitmap or Stream instance, it will be disposed after it has been used. Defaults to true.
        /// </summary>
        public bool DisposeSourceObject {
            get { return _disposeSourceObject; }
            set { _disposeSourceObject = value; }
        }

        private bool _resetSourceStream = false;
        /// <summary>
        /// If true, and if 'source' is seekable, the stream will be reset to its previous position after being read.
        /// Always true for HttpPostedFile(Base) instances, defaults to false for all others.
        /// </summary>
        public bool ResetSourceStream {
            get { return _resetSourceStream; }
            set { _resetSourceStream = value; }
        }

        private bool _disposeDestinationStream = false;
        /// <summary>
        /// If true, and if 'dest' is a Stream instance, it will be disposed after the image has been written. Defaults to false.
        /// </summary>
        public bool DisposeDestinationStream {
            get { return _disposeDestinationStream; }
            set { _disposeDestinationStream = value; }
        }

        private string _finalPath = null;
        /// <summary>
        /// Contains the final physical path to the image (if 'dest' was a path - null otherwise)
        /// </summary>
        public string FinalPath {
            get { return _finalPath; }
            set { _finalPath = value; }
        }


        private string _sourcePathData = null;
        /// <summary>
        /// If 'source' contains any path-related data, it is copied into this member for use by format detetction code, so decoding can be optimized.
        /// May be a physical or virtual path, or just a file name.
        /// </summary>
        public string SourcePathData {
            get { return _sourcePathData; }
            set { _sourcePathData = value; }
        }

        private bool _addFileExtension = false;
        /// <summary>
        /// If true, the appropriate extension for the encoding format will be added to the destination path, and the result will be stored in FinalPath in physical path form.
        /// </summary>
        public bool AddFileExtension {
            get { return _addFileExtension; }
            set { _addFileExtension = value; }
        }

        private bool _allowDestinationPathVariables = true;
        /// <summary>
        /// If true (the default), destination paths can include variables that are expanded during the image build process. 
        /// Ex. Dest = "~/folder/&lt>guid>.&lt;ext>" will expand to "C:\WWW\App\folder\1ddadaadaddaa75da75ad34ad33da3a.jpg". 
        /// </summary>
        public bool AllowDestinationPathVariables {
            get { return _allowDestinationPathVariables; }
            set { _allowDestinationPathVariables = value; }
        }

        private bool _createParentDirectory = false;
        /// <summary>
        /// Defaults to false. When true, the parent directory of the destination filename will be created if it doesn't already exist.
        /// </summary>
        public bool CreateParentDirectory {
            get { return _createParentDirectory; }
            set { _createParentDirectory = value; }
        }
        /// <summary>
        /// Internal use only.
        /// Resolves the specified (potenetially templated) path into a physical path. 
        /// Applies the AddFileExtension setting using the 'ext' variable.
        /// Supplies the guid, settings.*, filename, path, and originalExt variables. 
        /// The resolver method should supply 'ext', 'width', and 'height' (all of which refer to the final image).
        /// If AllowDestinationPathVariables=False, only AddFileExtenson will be processed.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="resolver"></param>
        /// <param name="originalWidth">Leave 0 if unavailable</param>
        /// <param name="originalHeight">Leave 0 if unavailable</param>
        /// <param name="builder">Leave null if unavailable</param>
        /// <returns></returns>
        public string ResolveTemplatedPath(string path, ImageResizer.Util.PathUtils.VariableResolverCallback resolver) {
            if (!AllowDestinationPathVariables) {
                //Only add the extension if requested when variables are turned off.
                return PathUtils.MapPathIfAppRelative(path + (AddFileExtension ? ("." + resolver("ext")) : ""));
            }
            if (this.AddFileExtension) path = path + ".<ext>";
            path = PathUtils.ResolveVariablesInPath(path, delegate(string p) {
                //Let the 'resolver' passed to this method take precedence - we provide default values.
                string result = resolver(p);
                if (result != null) return result;
                //GUID in lowercase hexadecimal with no hyphens
                if ("guid".Equals(p, StringComparison.OrdinalIgnoreCase)) 
                    return Guid.NewGuid().ToString("N"); 
                //Access to the settings collection
                string settingsPrefix = "settings.";
                if (p.StartsWith(settingsPrefix, StringComparison.OrdinalIgnoreCase)) {
                    string subName = p.Substring(settingsPrefix.Length);
                    return Settings[subName];
                }
                if ("filename".Equals(p,StringComparison.OrdinalIgnoreCase)){
                    if (SourcePathData == null) throw new ImageProcessingException("You cannot use the <filename> variable in a job that does not have a source filename, such as with a Stream or Bitmap instance");
                    return Path.GetFileNameWithoutExtension(SourcePathData);
                }
                if ("dir".Equals(p, StringComparison.OrdinalIgnoreCase)) {
                    if (SourcePathData == null) throw new ImageProcessingException("You cannot use the <dir> variable in a job that does not have a source filename, such as with a Stream or Bitmap instance");
                    return Path.GetDirectoryName(SourcePathData); //Just remove the last segment
                }
                if ("path".Equals(p, StringComparison.OrdinalIgnoreCase)) {
                    if (SourcePathData == null) throw new ImageProcessingException("You cannot use the <path> variable in a job that does not have a source filename, such as with a Stream or Bitmap instance");
                    return PathUtils.RemoveExtension(SourcePathData); //Just remove the last segment
                }
                if ("originalext".Equals(p, StringComparison.OrdinalIgnoreCase)) {
                    if (SourcePathData == null) throw new ImageProcessingException("You cannot use the <originalext> variable in a job that does not have a source filename, such as with a Stream or Bitmap instance");
                    return PathUtils.GetExtension(SourcePathData); //Just remove the last segment
                }
                

                return null;
            });
            return PathUtils.MapPathIfAppRelative(path);
        }


    }

}
