using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Linq;
using System.Windows.Forms;
using ImageResizerGUI.Code;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using ImageResizer;
using ImageResizer.Plugins.BatchZipper;
using System.Collections.ObjectModel;
using MessageBox = System.Windows.Forms.MessageBox;

namespace ImageResizerGUI
{
    public enum SaveMode
    {
        ModifyExisting, ExportResults, CreateZipFile
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region fields

        private string saveFolderPath;
        private string saveZipPath;
        private string openFolderPath;

        private BackgroundWorker bwResizeBatch;

        private BackgroundWorker bwResizeBatchAndZip;

        AdvancedOptions aOptions;

        private SaveMode saveMode;

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            aOptions = new AdvancedOptions(this);

            btn_BrowseButton.Click += btn_BrowseButton_Click;

            btn_addFiles.Click += btn_addFiles_Click;
            btn_remove.Click += btn_remove_Click;
            btn_clearAll.Click += btn_clearAll_Click;

            comboBox_exportAction.SelectionChanged += comboBox_exportAction_SelectionChanged;
            comboBox_exportAction.SelectedIndex = 1;
            saveMode = SaveMode.ExportResults;

            tbox_height.PreviewTextInput += tbox_PreviewTextInput;
            tbox_width.PreviewTextInput += tbox_PreviewTextInput;

            Closing += MainWindow_Closing;
        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!btn_viewResults.IsEnabled)
                if (
                    System.Windows.MessageBox.Show("Do you want to interrupt the current work?", "Program Close",
                                                   MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    if (bwResizeBatch != null)
                        bwResizeBatch.CancelAsync();

                    if (bwResizeBatchAndZip != null)
                        bwResizeBatchAndZip.CancelAsync();
                }
                else
                    e.Cancel = true;
        }

        /// <summary>
        /// Read from de UI the values for configure the resize.
        /// </summary>
        public ResizeSettings UserResizeSettings
        {
            get
            {
                var rs = new ResizeSettings();

                int height = int.Parse(tbox_height.Text);
                rs.MaxHeight = height;

                int width = int.Parse(tbox_width.Text);
                rs.MaxWidth = width;

                return rs;
            }
        }

        #region Events

        void tbox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            try
            {
                Convert.ToInt32(e.Text);
            }
            catch
            {
                e.Handled = true;
            }

        }

        void btn_BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            if (saveMode == SaveMode.ExportResults)
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.Description = "Open a destination folder";
                    dialog.SelectedPath = saveFolderPath ?? Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);
                    dialog.ShowNewFolderButton = true;
                    dialog.RootFolder = Environment.SpecialFolder.MyComputer;
                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        saveFolderPath = tbox_savePath.Text = dialog.SelectedPath;
                }
            }

            if (saveMode == SaveMode.CreateZipFile)
            {
                using (var dialog = new SaveFileDialog())
                {
                    dialog.InitialDirectory = saveFolderPath ?? Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);
                    dialog.Filter = "Zip File(*.ZIP)|*.ZIP";

                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        BatchInfo saveFile = new BatchInfo(dialog.FileName);
                        saveZipPath = tbox_savePath.Text = dialog.FileName;
                        saveFolderPath = saveFile.Folder;
                    }

                }
            }
        }

        void comboBox_exportAction_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            grid_exportResults.Visibility = Visibility.Collapsed;

            switch (comboBox_exportAction.SelectedIndex)
            {
                case 0:
                    saveMode = SaveMode.ModifyExisting;
                    break;
                case 1:
                    btn_BrowseButton.Content = "Browse";
                    saveMode = SaveMode.ExportResults;
                    grid_exportResults.Visibility = Visibility.Visible;
                    tbox_savePath.Text = saveFolderPath;
                    break;
                case 2:
                    btn_BrowseButton.Content = "Save as";
                    saveMode = SaveMode.CreateZipFile;
                    grid_exportResults.Visibility = Visibility.Visible;
                    tbox_savePath.Text = saveZipPath;
                    break;
            }
        }

        void btn_clearAll_Click(object sender, RoutedEventArgs e)
        {
            var allItems = new List<BatchInfo>();

            foreach (BatchInfo item in listView.Items)
                allItems.Add(item);

            foreach (BatchInfo item in allItems)
                listView.Items.Remove(item);
        }

        void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = new List<BatchInfo>();

            foreach (BatchInfo item in listView.SelectedItems)
                selectedItems.Add(item);

            foreach (BatchInfo item in selectedItems)
                listView.Items.Remove(item);
        }

        void btn_addFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog { Multiselect = true };


                var fileExtensions = ImageBuilder.Current.GetSupportedFileExtensions();
                string filter = "Image Files(*.BMP;*.JPG;*.GIF;*.PNG)|*.BMP;*.JPG;*.GIF;*.PNG";

                if (fileExtensions.Count() > 0)
                {
                    filter = "";
                    foreach (var fileExtension in fileExtensions)
                        filter += "*." + fileExtension.ToUpper() + ";";
                    filter.Remove(filter.Length - 1);

                    filter = "Image Files(" + filter + ")|" + filter;
                }


                openFileDialog.Filter = filter;
                openFileDialog.ShowDialog(this);

                if (openFileDialog.FileNames.Length > 0)
                {
                    var duplicatedImages = new List<string>();

                    foreach (var fileName in openFileDialog.FileNames)
                        if (!ImageInserted(fileName))
                            listView.Items.Add(new BatchInfo(fileName));
                        else
                            duplicatedImages.Add(fileName);

                    if (duplicatedImages.Count > 0)
                        MessageBox.Show(duplicatedImages.Count + " files are already inserted.", "Duplicated Items");
                }
            }
            catch
            { }
        }

        private void btn_resize_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckFileSettings())
                return;

            gridResults.Visibility = Visibility.Visible;
            tbStatus.Text = "Status:";

            switch (saveMode)
            {
                case SaveMode.ModifyExisting:
                case SaveMode.ExportResults:
                    ResizeBatch();
                    break;

                case SaveMode.CreateZipFile:
                    var bInfo = new BatchInfo(saveZipPath);
                    if (File.Exists(saveZipPath))
                        File.Delete(saveZipPath);
                    ResizeBatchAndZip();
                    break;
            }


        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            gridResults.Visibility = Visibility.Collapsed;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (bwResizeBatch != null)
                bwResizeBatch.CancelAsync();

            if (bwResizeBatchAndZip != null)
                bwResizeBatchAndZip.CancelAsync();

            // Turn off from here the progress bar and cancel button and report that.
            tbStatus.Text = "Status : Canceled";

            btnCancel.Visibility =
            pBar1.Visibility = Visibility.Hidden;

        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            aOptions.SetData(UserResizeSettings.MaxHeight, UserResizeSettings.MaxWidth);
            aOptions.ShowDialog();
        }

        private void btn_addFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Add image files recursively";
                dialog.ShowNewFolderButton = true;
                dialog.RootFolder = Environment.SpecialFolder.MyComputer;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var files = FileTools.GetFilesRecursive(dialog.SelectedPath);

                    var duplicatedImages = new List<string>();

                    foreach (var fileName in files)
                        if (!ImageInserted(fileName))
                            listView.Items.Add(new BatchInfo(fileName));
                        else
                            duplicatedImages.Add(fileName);

                    if (duplicatedImages.Count > 0)
                        MessageBox.Show(duplicatedImages.Count + " files are already inserted.", "Duplicated Items");
                }
            }

        }

        private void btn_viewResults_Click(object sender, RoutedEventArgs e)
        {
            string path = (string.IsNullOrEmpty(saveFolderPath)) ? @"c:\" : saveFolderPath;
            System.Diagnostics.Process.Start(path);
        }

        #endregion

        #region methods

        void ResizeBatch()
        {
            // Prepare all controls for image processing...
            btnCancel.Visibility = pBar1.Visibility = Visibility.Visible;
            btn_back.IsEnabled = false;
            btn_viewResults.IsEnabled = false;

            var resultItems = new ObservableCollection<BatchInfo>();
            foreach (var item in listView.Items)
            {
                ((BatchInfo)item).Status = 0;
                resultItems.Add(((BatchInfo)item));
            }
            dataGridResults.ItemsSource = resultItems;

            bwResizeBatch = new BackgroundWorker();
            bwResizeBatch.DoWork += BwResizeBatch_DoWork;
            bwResizeBatch.RunWorkerCompleted += BwResizeBatch_RunWorkerCompleted;
            bwResizeBatch.WorkerReportsProgress = true;
            bwResizeBatch.ProgressChanged += BwResizeBatch_ProgressChanged;
            bwResizeBatch.WorkerSupportsCancellation = true;

            var batchItems = new List<BatchInfo>();

            foreach (var item in listView.Items)
            {
                batchItems.Add(new BatchInfo((BatchInfo)item));
            }

            bwResizeBatch.RunWorkerAsync(new Dictionary<string, object>()
                {
                    { "MaxHeight",  UserResizeSettings.MaxHeight},
                    { "MaxWidth", UserResizeSettings.MaxWidth },
                    { "batchItems", batchItems }
                });
        }

        void ResizeBatchAndZip()
        {
            // Prepare all controls for image processing...
            btnCancel.Visibility = pBar1.Visibility = Visibility.Visible;
            btn_back.IsEnabled = false;
            btn_viewResults.IsEnabled = false;

            var resultItems = new ObservableCollection<BatchInfo>();
            foreach (var item in listView.Items)
            {
                ((BatchInfo)item).Status = 0;
                resultItems.Add(((BatchInfo)item));
            }
            dataGridResults.ItemsSource = resultItems;

            bwResizeBatchAndZip = new BackgroundWorker();
            bwResizeBatchAndZip.DoWork += bwResizeBatchAndZip_DoWork;
            bwResizeBatchAndZip.RunWorkerCompleted += bwResizeBatchAndZip_RunWorkerCompleted;
            bwResizeBatchAndZip.WorkerReportsProgress = true;
            bwResizeBatchAndZip.ProgressChanged += bwResizeBatchAndZip_ProgressChanged;
            bwResizeBatchAndZip.WorkerSupportsCancellation = true;

            var batchItems = new List<BatchInfo>();

            foreach (var item in listView.Items)
            {
                batchItems.Add(new BatchInfo((BatchInfo)item));
            }

            bwResizeBatchAndZip.RunWorkerAsync(new Dictionary<string, object>()
                {
                    { "MaxHeight",  UserResizeSettings.MaxHeight},
                    { "MaxWidth", UserResizeSettings.MaxWidth },
                    { "batchItems", batchItems }
                });
        }

        bool ImageInserted(string imageFullPath)
        {
            var items = from object item in listView.Items
                        where ((BatchInfo)item).FullPath == imageFullPath
                        select item;

            if (items.Count() > 0)
                return true;
            return false;
        }

        bool CheckFileSettings()
        {
            if (listView.Items.Count == 0)
            {
                System.Windows.MessageBox.Show("You must add a file.");
                return false;
            }

            if (saveMode == SaveMode.ExportResults)
            {
                if (string.IsNullOrEmpty(saveFolderPath))
                {
                    System.Windows.MessageBox.Show("You must select a destination folder.");
                    return false;
                }

            }

            if (saveMode == SaveMode.CreateZipFile)
            {
                if (string.IsNullOrEmpty(saveZipPath))
                {
                    System.Windows.MessageBox.Show("You must select a destination ZIP file.");
                    return false;
                }
                var bInfo = new BatchInfo(saveZipPath);
                if (!Directory.Exists(bInfo.Folder))
                {
                    System.Windows.MessageBox.Show("You must select a correct Directory. \"" + bInfo.Folder);
                    return false;
                }
                if (File.Exists(saveZipPath))
                {
                    if (System.Windows.MessageBox.Show("The ZIP file already exists. \"" + bInfo.Folder + ". Do you want to replace this file?", "Replace Files", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                        return false;
                }

            }
            return true;
        }

        #endregion

        #region BatchZip events

        void s_ItemEvent(ItemEventArgs e)
        {
            if (bwResizeBatchAndZip.CancellationPending)
            {
                e.Cancel = true;
                tbStatus.Dispatcher.BeginInvoke(new Action(() => tbStatus.Text = "Status: Cancelled by user"));
                return;
            }
            bwResizeBatchAndZip.ReportProgress((e.Stats.SuccessfulItems * 100) / e.Stats.RequestedItems, e.Result.Item.PhysicalPath);
        }

        void s_JobEvent(JobEventArgs e)
        {
        }

        #endregion

        #region BackgroundWorker Events

        void bwResizeBatchAndZip_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                Dictionary<string, object> UserInputs = e.Argument as Dictionary<string, object>;

                if (UserInputs != null)
                {
                    int maxHeight = (int)UserInputs["MaxHeight"];
                    int maxWidth = (int)UserInputs["MaxWidth"];
                    var batchItems = (List<BatchInfo>)UserInputs["batchItems"];

                    Guid job = Guid.NewGuid();
                    BatchResizeSettings s = new BatchResizeSettings(saveZipPath, job, new List<BatchResizeItem>());

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

            var itemDone = ((string)e.UserState);

            var query = from object item in dataGridResults.Items
                        where ((BatchInfo)item).FullPath == itemDone
                        select item;

            ((BatchInfo)query.First()).Status = 100;
        }

        void bwResizeBatchAndZip_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btnCancel.Visibility =
            pBar1.Visibility = Visibility.Hidden;
            btn_back.IsEnabled = true;
            btn_viewResults.IsEnabled = true;
        }

        private void BwResizeBatch_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
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

                        if (saveMode == SaveMode.ModifyExisting)
                            ImageBuilder.Current.Build(item.FullPath, item.FullPath, rs);
                        else
                            ImageBuilder.Current.Build(item.FullPath, saveFolderPath + item.FileName, rs);

                        item.Status = 100;

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

            ((BatchInfo)query.First()).Status = 100;
        }

        private void BwResizeBatch_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btnCancel.Visibility =
            pBar1.Visibility = Visibility.Hidden;
            btn_back.IsEnabled = true;
            btn_viewResults.IsEnabled = true;
        }

        #endregion


    }
}
