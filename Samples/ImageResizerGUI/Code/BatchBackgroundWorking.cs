using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using ImageResizer;

namespace ImageResizerGUI.Code
{
    public class BatchBackgroundWorking
    {
        public event ProgressChangedEventHandler ProgressChangedEvent;
        public event DoWorkEventHandler DoWorkEvent;
        public event RunWorkerCompletedEventHandler RunWorkerCompletedEvent;

        private BackgroundWorker bwResizeBatch;
        private string querystring;
        private List<BatchInfo> items;
        private SaveMode saveMode;

        public BatchBackgroundWorking(string querystring, List<BatchInfo> items, SaveMode saveMode)
        {
            this.querystring = querystring;
            this.items = items;
            this.saveMode = saveMode;

            bwResizeBatch = new BackgroundWorker
                                {
                                    WorkerReportsProgress = true,
                                    WorkerSupportsCancellation = true
                                };

            bwResizeBatch.DoWork += bwResizeBatch_DoWork;
            bwResizeBatch.ProgressChanged += bwResizeBatch_ProgressChanged;
            bwResizeBatch.RunWorkerCompleted += bwResizeBatch_RunWorkerCompleted;
        }

        public void InitWork()
        {
            var batchItems = new List<BatchInfo>();

            // duplicate item to avoid mutiple threads accessing to the same object.
            foreach (var item in items)
                batchItems.Add(new BatchInfo(item));


            bwResizeBatch.RunWorkerAsync(new Dictionary<string, object>
                                             {
                    { "querystring",  querystring},
                    { "batchItems", batchItems }
                });
        }

        public void StopWork()
        {
            bwResizeBatch.CancelAsync();
        }

        void bwResizeBatch_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (RunWorkerCompletedEvent != null)
                RunWorkerCompletedEvent(this, new RunWorkerCompletedEventArgs(e.Result, e.Error, e.Cancelled));
        }

        void bwResizeBatch_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (ProgressChangedEvent != null)
                ProgressChangedEvent(this, new ProgressChangedEventArgs(e.ProgressPercentage, e.UserState));
        }

        void bwResizeBatch_DoWork(object sender, DoWorkEventArgs e)
        {
            if (DoWorkEvent != null)
                DoWorkEvent(this, new DoWorkEventArgs(e.Argument));

            try
            {
                Dictionary<string, object> UserInputs = e.Argument as Dictionary<string, object>;

                if (UserInputs != null)
                {
                    ResizeSettings rs = new ResizeSettings((string)UserInputs["querystring"]);
                    var batchItems = (List<BatchInfo>)UserInputs["batchItems"];


                    int count = 0;
                    foreach (var item in batchItems)
                    {
                        count++;
                        try
                        {
                            if (saveMode == SaveMode.ModifyExisting) // if the savepath have the default value, replace the existing image.
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

                        }

                        bwResizeBatch.ReportProgress((count * 100) / batchItems.Count, item);

                        if (bwResizeBatch.CancellationPending)
                        {
                            e.Cancel = true;
                            return;
                        }
                    }

                    e.Result = batchItems; // Pass the results to the completed events to process them accordingly.
                }
            }
            catch (Exception ex)
            {
                // handle the exception.
            }
        }

    }
}
