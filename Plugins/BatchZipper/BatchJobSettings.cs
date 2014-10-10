/* Copyright (c) 2014 Imazen See license.txt for your rights */
using System;
using System.Threading;
using System.Collections.Generic;
using Ionic.Zip;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using ImageResizer.Configuration;
using ImageResizer;

namespace ImageResizer.Plugins.BatchZipper
{
    public delegate void ItemCallback(ItemEventArgs e);

    public delegate void JobCallback(JobEventArgs e);

    /// <summary>
    /// Represents a file to resize/re-encode, the resize/encoding settings
    /// </summary>
    public class BatchResizeItem
    {
        /// <summary>
        /// Represents a file to resize/re-encode, the resize/encoding settings
        /// </summary>
        /// <param name="physicalPath">Filesystem path, such as C:\Web\Project\Image\file.jpg</param>
        /// <param name="targetFilename">The target filename to use in the compressed folder. If left null, the filename of physicalPath will be used.
        /// Should not include the file extension, that will be assigned based on resizeQuerystring and the original extension.</param>
        /// <param name="resizeQuerystring">The resize/crop settings applied to the file, ex. ?width=100&amp;height=100&amp;crop=auto&amp;format=png</param>
         public BatchResizeItem(string physicalPath, string targetFilename, string resizeQuerystring)
        {
            this.physicalPath = physicalPath;
            this.resizeQuerystring = resizeQuerystring;
            this.targetFilename = targetFilename;
        }
        /// <summary>
        /// Represents a file to resize/re-encode, the resize/encoding settings
        /// </summary>
        /// <param name="physicalPath">Filesystem path, such as C:\Web\Project\Image\file.jpg</param>
        /// <param name="targetFilename">The target filename to use in the compressed folder. If left null, the filename of physicalPath will be used.
        /// Should not include the file extension, that will be assigned based on resizeQuerystring and the original extension.</param>
        /// <param name="resizeQuerystring">The resize/crop settings applied to the file, ex. ?width=100&amp;height=100&amp;crop=auto&amp;format=png</param>
        /// <param name="mutable">If false, an InvalidOperationException exception will be thrown when you attempt to modify the instance. Set this to true when building a job.</param>
        protected internal BatchResizeItem(string physicalPath, string targetFilename, string resizeQuerystring, bool mutable)
        {
            this.physicalPath = physicalPath;
            this.resizeQuerystring = resizeQuerystring;
            this.targetFilename = targetFilename;
            this.mutable = mutable;
        }

        private string physicalPath;
        /// <summary>
        /// Filesystem path, such as C:\Web\Project\Image\file.jpg. Throws an InvalidOperationException if you attempt to modify an immutable instance. 
        /// </summary>
        public string PhysicalPath
        {
            get { return physicalPath; }
            set { ThowIfInvalid(); physicalPath = value; }
        }


        private string resizeQuerystring;
        /// <summary>
        /// The resize/crop settings applied to the file, ex. ?width=100&amp;height=100&amp;crop=auto&amp;format=png. Throws an InvalidOperationException if you attempt to modify an immutable instance. 
        /// </summary>
        public string ResizeQuerystring
        {
            get { return resizeQuerystring; }
            set { ThowIfInvalid(); resizeQuerystring = value; }
        }


        private string targetFilename;
        /// <summary>
        /// The target filename to use in the compressed folder. If left null, the filename of physicalPath will be used.
        /// Should not include the file extension, that will be assigned based on resizeQuerystring and the original extension.
        /// Throws an InvalidOperationException if you attempt to modify an immutable instance. 
        /// </summary>
        public string TargetFilename
        {
            get { return targetFilename; }
            set { ThowIfInvalid(); targetFilename = value; }
        }
        /// <summary>
        /// Throws an error if the instance is immutable.
        /// </summary>
        protected void ThowIfInvalid()
        {
            if (!mutable) throw new InvalidOperationException("This instance is marked as immutable.");
        }

        protected bool mutable = true;

        /// <summary>
        /// Copies the class, useful for preventing threading issues. Lightweight, only 4 pointers plus class instance.
        /// </summary>
        /// <returns></returns>
        public BatchResizeItem Copy()
        {
            return new BatchResizeItem(PhysicalPath, TargetFilename, ResizeQuerystring, mutable);
        }
        /// <summary>
        /// Blocks any future changes to the instance, throwing an InvalidOperationException.
        /// </summary>
        public void SetImmutable()
        {
            mutable = false;
        }

        public override string ToString()
        {
            return base.ToString() + "{PhysicalPath=\"" + PhysicalPath + "\", TargetFilename=\"" + TargetFilename + "\", ResizeQuerystring=\"" + ResizeQuerystring + "\", mutable=" + mutable + "}";
        }
    }

    /// <summary>
    /// Use this to configure the a resize job. After you start the job, do not modify this instance.
    /// </summary>
    public class BatchResizeSettings
    {
        /// <summary>
        /// Use this to configure a batch resize job. Mutable.
        /// Remember to assign at least a JobEvent handler so you know the fate of the job.
        /// </summary>
        /// <param name="jobId">The job ID, can be generated with Guid.NewGuid() </param>
        /// <param name="destinationFile">The physical path to the destination archive</param>
        /// <param name="files">A List of items to to resize and place in the folder.</param>
        public BatchResizeSettings(string destinationFile, Guid jobId, IList<BatchResizeItem> files)
        {
            this.jobId = jobId;
            this.destinationFile = destinationFile;
            this.files = files;
            this.conf = Config.Current;
        }
        //Input settings
        public Guid jobId;
        public IList<BatchResizeItem> files;
        public string destinationFile;

        public Config conf = null;

        //Progress/failure callbacks
        /// <summary>
        /// Fired when a file is successfully written to the zip file, or if an item fails to be added to the zip file.
        /// Will execute on a thread pool thread. Catch all your exeptions, or they will cause the jobe to fail.
        /// Set e.Cancel to cancel the job. 
        /// Uses the same thread the job is processing on - I/O bound tasks in handlers should be async if possible.
        /// </summary>
        public event ItemCallback ItemEvent; //Called when a item is sucessfully written to the zip file, or when it fails
        /// <summary>
        /// Fires when the Zip file has successfully been written to disk, when the job fails.
        /// Will execute on a thread pool thread. Catch all your exeptions, or they will cause the asp.net proccess to restart!
        /// </summary>
        public event JobCallback JobEvent;

        protected internal void FireItemEvent(ItemEventArgs e)
        {
            if (ItemEvent != null) ItemEvent(e);
        }
        protected internal void FireJobEvent(JobEventArgs e)
        {
            if (JobEvent != null) JobEvent(e);
        }

        /// <summary>
        /// Loops through all files, assigning targetFilenames if they are null,
        /// and eliminating duplicate names by adding _1, _2, etc.
        /// Also normalizes filenames for use in zip folder.
        /// Used internally. May be used externally also if calling code wishes to to know the final file names.
        /// </summary>
        public void FixDuplicateFilenames(string prefix = "_")
        {
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (BatchResizeItem i in files)
            {
                //Assign default if null
                if (i.TargetFilename == null) i.TargetFilename = System.IO.Path.GetFileNameWithoutExtension(i.PhysicalPath);

                //See if it is a duplicate
                if (names.Contains(i.TargetFilename))
                {
                    //Generate a unique filename by appending a number.
                    int index = 1;
                    string next = i.TargetFilename + prefix + index;
                    while (names.Contains(next))
                    {
                        index++;
                        next = i.TargetFilename + prefix + index;
                    }
                    i.TargetFilename = next;
                }

                //Normalize for zip usage
                i.TargetFilename = NormalizePathForUseInZipFile(i.TargetFilename);

                //Add to names set, so we can verify future filename uniqueness.
                names.Add(i.TargetFilename);

            }
        }
        /// <summary>
        /// Utility routine for transforming path names from filesystem format (on Windows that means backslashes) to
        /// a format suitable for use within zipfiles. This means trimming the volume letter and colon (if any) And
        /// swapping backslashes for forward slashes.
        /// </summary>
        /// <param name="pathName">source path.</param>
        /// <returns>transformed path</returns>
        public static string NormalizePathForUseInZipFile(string pathName)
        {
            // boundary case
            if (String.IsNullOrEmpty(pathName)) return pathName;

            // trim volume if necessary
            if ((pathName.Length >= 2) && ((pathName[1] == ':') && (pathName[2] == '\\')))
                pathName = pathName.Substring(3);

            // swap slashes
            pathName = pathName.Replace('\\', '/');

            // trim all leading slashes
            while (pathName.StartsWith("/")) pathName = pathName.Substring(1);

            return SimplifyFwdSlashPath(pathName);
        }

        private static string SimplifyFwdSlashPath(string path)
        {
            if (path.StartsWith("./")) path = path.Substring(2);
            path = path.Replace("/./", "/");
            // Replace foo/anything/../bar with foo/bar
            var re = new System.Text.RegularExpressions.Regex(@"^(.*/)?([^/\\.]+/\\.\\./)(.+)$");
            path = re.Replace(path, "$1$3");
            return path;
        }


        /// <summary>
        /// Adds the correct, final extension  to each target file name.Internal use only.
        /// </summary>
        internal void AppendFinalExtensions()
        {
            foreach (BatchResizeItem i in files)
            {
                string originalName = System.IO.Path.GetFileName(i.PhysicalPath);
                //Can we resize it?
                if (conf.Pipeline.IsAcceptedImageType(originalName))
                {
                    //Add the correct (possibly changed) file extension.
                    i.TargetFilename += "." + conf.Plugins.EncoderProvider.GetEncoder(new ResizeSettings(i.ResizeQuerystring), originalName).Extension;
                }
                else
                {
                    //Just keep the same extension.
                    i.TargetFilename += System.IO.Path.GetExtension(originalName); //(includes the ".")
                }

            }
        }


        internal void MarkItemsImmutable()
        {
            foreach (BatchResizeItem i in files)
            {
                i.SetImmutable();
            }
        }
    }
}