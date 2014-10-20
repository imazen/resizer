using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.ComponentModel;
using ImageResizerGUI.Code;
using ImageResizer.Plugins.BatchZipper;
using System.IO;
using ImageResizer;

namespace ImageResizerGUI
{
    /// <summary>
    /// Partial code for BackgroundWorkers usage
    /// </summary>
    public partial class MainWindow : Window
    {
        //private BackgroundWorker bwResizeBatchAndZip;
        void batchBackgroundWorking_RunWorkerCompletedEvent(object sender, RunWorkerCompletedEventArgs e)
        {
            btnCancel.Visibility =
            pBar1.Visibility = Visibility.Hidden;
            btn_back.IsEnabled = true;
            btn_viewResults.IsEnabled = true;

            if (!string.IsNullOrEmpty(Properties.Settings.Default.saveFolderPath) && Directory.Exists(Properties.Settings.Default.saveFolderPath) && saveMode != SaveMode.ModifyExisting)
                System.Diagnostics.Process.Start(Properties.Settings.Default.saveFolderPath);
        }

        void batchBackgroundWorking_ProgressChangedEvent(object sender, ProgressChangedEventArgs e)
        {
            pBar1.Visibility = Visibility.Visible;
            pBar1.Value = e.ProgressPercentage;

            var itemDone = ((BatchInfo)e.UserState);

            var query = from object item in dataGridResults.Items
                        where ((BatchInfo)item).FullPath == itemDone.FullPath
                        select item;

            ((BatchInfo)query.First()).StatusText = itemDone.StatusText;
            ((BatchInfo)query.First()).Status = itemDone.Status;
        }

        void batchBackgroundWorking_DoWorkEvent(object sender, DoWorkEventArgs e)
        {
            // Prepare all controls for image processing...
            btnCancel.Dispatcher.BeginInvoke(new Action(() => btnCancel.Visibility = Visibility.Visible));
            pBar1.Dispatcher.BeginInvoke(new Action(() => pBar1.Visibility = Visibility.Visible));
            btn_back.Dispatcher.BeginInvoke(new Action(() => btn_back.IsEnabled = false));
            btn_viewResults.Dispatcher.BeginInvoke(new Action(() => btn_viewResults.IsEnabled = false));
        }


        private void zipBackgroundWorking_DoWorkEvent(object sender, DoWorkEventArgs e)
        {
            // Prepare all controls for image processing...
            btnCancel.Dispatcher.BeginInvoke(new Action(() => btnCancel.Visibility = Visibility.Visible));
            pBar1.Dispatcher.BeginInvoke(new Action(() => pBar1.Visibility = Visibility.Visible));
            btn_back.Dispatcher.BeginInvoke(new Action(() => btn_back.IsEnabled = false));
            btn_viewResults.Dispatcher.BeginInvoke(new Action(() => btn_viewResults.IsEnabled = false));
        }

        private void zipBackgroundWorking_ProgressChangedEvent(object sender, ProgressChangedEventArgs e)
        {
            pBar1.Visibility = Visibility.Visible;
            pBar1.Value = e.ProgressPercentage;

            var itemDone = ((BatchInfo)e.UserState);

            var query = from object item in dataGridResults.Items
                        where ((BatchInfo)item).FullPath == itemDone.FullPath
                        select item;

            ((BatchInfo)query.First()).StatusText = "Done";
            ((BatchInfo)query.First()).Status = 100;
        }

        private void zipBackgroundWorking_RunWorkerCompletedEvent(object sender, RunWorkerCompletedEventArgs e)
        {
            btnCancel.Visibility = pBar1.Visibility = Visibility.Hidden;
            btn_back.IsEnabled = true;
            btn_viewResults.IsEnabled = true;

            if (!string.IsNullOrEmpty(Properties.Settings.Default.saveZipPath) && File.Exists(Properties.Settings.Default.saveZipPath))
                System.Diagnostics.Process.Start(new BatchInfo(Properties.Settings.Default.saveZipPath).Folder);
        }
    }
}
