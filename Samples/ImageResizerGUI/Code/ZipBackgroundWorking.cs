using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows;
using ImageResizer;
using ImageResizer.Plugins.BatchZipper;

namespace ImageResizerGUI.Code
{
    class ZipBackgroundWorking
    {
        public event ProgressChangedEventHandler ProgressChangedEvent;
        public event DoWorkEventHandler DoWorkEvent;
        public event RunWorkerCompletedEventHandler RunWorkerCompletedEvent;

        private BackgroundWorker bwResizeBatch;
        private string querystring;
        private List<BatchInfo> items;
        private List<BatchInfo> batchItems;
        private SaveMode saveMode;

        private int count;
        private int total;

        public ZipBackgroundWorking(string querystring, List<BatchInfo> items, SaveMode saveMode)
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
            count++;

            if (ProgressChangedEvent != null)
                ProgressChangedEvent(this, new ProgressChangedEventArgs((count * 100) / total, items[count - 1]));
        }

        void bwResizeBatch_DoWork(object sender, DoWorkEventArgs e)
        {
            if (DoWorkEvent != null)
                DoWorkEvent(this, new DoWorkEventArgs(e.Argument));

            count = 0;

            try
            {
                Dictionary<string, object> UserInputs = e.Argument as Dictionary<string, object>;

                if (UserInputs != null)
                {
                    ResizeSettings rs = new ResizeSettings((string)UserInputs["querystring"]);
                    batchItems = (List<BatchInfo>)UserInputs["batchItems"];

                    total = batchItems.Count;

                    Guid job = Guid.NewGuid();

                    var s = new BatchResizeSettings(Properties.Settings.Default.saveZipPath, job, new List<BatchResizeItem>());

                    foreach (var item in batchItems)
                        s.files.Add(new BatchResizeItem(item.FullPath, null, rs.ToStringEncoded()));

                    s.ItemEvent += s_ItemEvent;
                    s.JobEvent += s_JobEvent;

                    new BatchResizeWorker(s).Work();
                }
            }
            catch (Exception ex)
            {
                // handle the exception.
            }
        }

        void s_ItemEvent(ItemEventArgs e)
        {
            var itemDone = e.Result;
            bwResizeBatch.ReportProgress(0);
        }

        void s_JobEvent(JobEventArgs e)
        {
            if (RunWorkerCompletedEvent != null)
                RunWorkerCompletedEvent(this, new RunWorkerCompletedEventArgs(e.Result, null, false));
        }
    }
}
