using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Media;

namespace ImageResizerGUI.Code
{
    /// <summary>
    /// BatchInfo objects stores informations about the images to resize.
    /// </summary>
    public class BatchInfo : INotifyPropertyChanged
    {
        private string fileName;
        private string fullPath;
        private int status;
        private string statusText;
        private string folder;

        /// <summary>
        /// Create a BatchItem from a path.
        /// </summary>
        /// <param name="fullPath"></param>
        public BatchInfo(string fullPath)
        {
            this.fullPath = fullPath;

            var arr = fullPath.Split('\\');
            fileName = arr[arr.Length - 1];
            if (fileName != "")
                folder = fullPath.Replace(FileName, "");
            else
                folder = fullPath;
            Status = 0;
            statusText = "Pending";
        }

        /// <summary>
        /// Duplicate (Clone) a BatchInfo.
        /// </summary>
        /// <param name="other"></param>
        public BatchInfo(BatchInfo other)
            : this(other.FullPath)
        {
        }

        /// <summary>
        /// Gets the file name .
        /// </summary>
        public string FileName
        {
            get { return fileName; }
        }

        /// <summary>
        /// Gets the full path
        /// </summary>
        public string FullPath
        {
            get { return fullPath; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Folder { get { return folder; } }

        public int Status
        {
            get { return status; }
            set
            {
                status = value;
                NotifyPropertyChanged("Status");
                NotifyPropertyChanged("Color");
            }
        }

        public string StatusText
        {
            get { return statusText; }
            set
            {
                statusText = value;
                NotifyPropertyChanged("StatusText");
            }
        }

        public Brush Color
        {
            get
            {
                if (Status == 50)
                    return new SolidColorBrush(Colors.Red);
                return new SolidColorBrush(Colors.Black);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }
}
