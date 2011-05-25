using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Media;

namespace ImageResizerGUI.Code
{
    public class BatchInfo : INotifyPropertyChanged
    {
        #region Fields

        private string fileName;
        private string fullPath;
        private int status;
        private string statusText;

        #endregion

        public BatchInfo(string fullPath)
        {
            FullPath = fullPath;

            var arr = fullPath.Split('\\');
            FileName = arr[arr.Length - 1];
            Folder = fullPath.Replace(FileName, "");
            Status = 0;
            statusText = "Pending";
        }

        public BatchInfo(BatchInfo other)
            : this(other.FullPath)
        {
        }

        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        public string FullPath
        {
            get { return fullPath; }
            set { fullPath = value; }
        }

        public string Folder { get; set; }

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
