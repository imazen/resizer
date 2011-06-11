using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private BackgroundWorker bwResizeBatch;

        private BackgroundWorker bwResizeBatchAndZip;

        void s_ItemEvent(ItemEventArgs e)
        {
            if (bwResizeBatchAndZip.CancellationPending)
            {
                e.Cancel = true;
                tbStatus.Dispatcher.BeginInvoke(new Action(() => tbStatus.Text = "Status: Cancelled by user"));
                return;
            }

            bwResizeBatchAndZip.ReportProgress((e.Stats.SuccessfulItems * 100) / e.Stats.RequestedItems, e.Result);
        }

        void s_JobEvent(JobEventArgs e)
        {
        }

        void bwResizeBatchAndZip_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                failedItems = new List<string>();

                Dictionary<string, object> UserInputs = e.Argument as Dictionary<string, object>;

                if (UserInputs != null)
                {
                    int maxHeight = (int)UserInputs["MaxHeight"];
                    int maxWidth = (int)UserInputs["MaxWidth"];
                    var batchItems = (List<BatchInfo>)UserInputs["batchItems"];

                    Guid job = Guid.NewGuid();
                    BatchResizeSettings s = new BatchResizeSettings(Properties.Settings.Default.saveZipPath, job, new List<BatchResizeItem>());

                    foreach (var batchItem in batchItems)
                    {
                        s.files.Add(new BatchResizeItem(batchItem.FullPath, null, "?maxwidth=" + maxWidth + "&maxheight=" + maxHeight));
                    }

                    s.JobEvent += s_JobEvent;
                    s.ItemEvent += s_ItemEvent;

                    //Executes on a thread pool thread
                    new BatchResizeWorker(s).Work();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                tbStatus.Dispatcher.BeginInvoke(new Action(() => tbStatus.Text = "Status: Error in the current operation"));
            }


        }

        void bwResizeBatchAndZip_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            pBar1.Visibility = Visibility.Visible;
            pBar1.Value = e.ProgressPercentage;

            var itemDone = ((ItemResult)e.UserState);

            if (failedItems.Contains(itemDone.Item.PhysicalPath))
                return;

            var query = from object item in dataGridResults.Items
                        where ((BatchInfo)item).FullPath == itemDone.Item.PhysicalPath
                        select item;

            //if (itemDone.ItemError == null)
            if (itemDone.Successful)
            {
                ((BatchInfo)query.First()).Status = 100;
                ((BatchInfo)query.First()).StatusText = "Done";

            }
            else
            {
                ((BatchInfo)query.First()).Status = 50;
                ((BatchInfo)query.First()).StatusText = "Error: " + itemDone.ItemError.Message;

                failedItems.Add(itemDone.Item.PhysicalPath);
            }



        }

        void bwResizeBatchAndZip_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btnCancel.Visibility =
            pBar1.Visibility = Visibility.Hidden;
            btn_back.IsEnabled = true;
            btn_viewResults.IsEnabled = true;

            if (failedItems.Count == 0 && !string.IsNullOrEmpty(Properties.Settings.Default.saveFolderPath) && Directory.Exists(Properties.Settings.Default.saveFolderPath))
                System.Diagnostics.Process.Start(Properties.Settings.Default.saveFolderPath);
        }

        private void BwResizeBatch_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                failedItems = new List<string>();

                Dictionary<string, object> UserInputs = e.Argument as Dictionary<string, object>;

                if (UserInputs != null)
                {
                    int maxHeight = (int)UserInputs["MaxHeight"];
                    int maxWidth = (int)UserInputs["MaxWidth"];
                    ResizeSettings rs = new ResizeSettings { MaxHeight = maxHeight, MaxWidth = maxWidth };

                    var batchItems = (List<BatchInfo>)UserInputs["batchItems"];

                    int count = 0;
                    foreach (var item in batchItems)
                    {
                        count++;
                        try
                        {
                            if (saveMode == SaveMode.ModifyExisting)
                                ImageBuilder.Current.Build(item.FullPath, item.FullPath, rs);
                            else
                                ImageBuilder.Current.Build(item.FullPath, Properties.Settings.Default.saveFolderPath + "\\" + item.FileName, rs);

                            item.StatusText = "Done";
                            item.Status = 100;
                        }
                        catch (Exception ex)
                        {
                            if (ex is ImageMissingException)
                            {
                                item.StatusText = "Error: Image missing.";
                                item.Status = 50;
                            }
                            else if (ex is ImageCorruptedException)
                            {
                                item.StatusText = "Error: Image corrupted";
                                item.Status = 50;
                            }
                            else
                            {
                                item.StatusText = "Error: " + ex.Message;
                                item.Status = 50;
                            }

                            failedItems.Add(item.FullPath);
                        }

                        bwResizeBatch.ReportProgress((count * 100) / batchItems.Count, item);

                        if (bwResizeBatch.CancellationPending)
                        {
                            e.Cancel = true;
                            tbStatus.Dispatcher.BeginInvoke(new Action(() => tbStatus.Text = "Status: Cancelled by user"));
                            return;
                        }
                    }

                    e.Result = batchItems; // Pass the results to the completed events to process them accordingly.
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                tbStatus.Dispatcher.BeginInvoke(new Action(() => tbStatus.Text = "Status: Error in the current operation"));
            }
        }

        private void BwResizeBatch_ProgressChanged(object sender, ProgressChangedEventArgs e)
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

        private void BwResizeBatch_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btnCancel.Visibility =
            pBar1.Visibility = Visibility.Hidden;
            btn_back.IsEnabled = true;
            btn_viewResults.IsEnabled = true;

            if (failedItems.Count == 0 && !string.IsNullOrEmpty(Properties.Settings.Default.saveFolderPath) && Directory.Exists(Properties.Settings.Default.saveFolderPath))
                System.Diagnostics.Process.Start(Properties.Settings.Default.saveFolderPath);
        }

    }
}
