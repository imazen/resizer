using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer {

    public class ImageJob {
        public ImageJob(string sourcePath, string destPath, ResizeSettings settings) {
            this.Source = sourcePath;
            this.Dest = destPath;
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
            this.DisposeSourceStream = disposeSource;
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

        private bool _disposeSourceStream = true;
        /// <summary>
        /// If true, and if 'source' is a Stream instance, it will be disposed after it has been read. Defaults to true.
        /// </summary>
        public bool DisposeSourceStream {
            get { return _disposeSourceStream; }
            set { _disposeSourceStream = value; }
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
    }

}
