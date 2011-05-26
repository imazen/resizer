using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Linq;
using System.Windows.Forms;
using ImageResizer.Configuration;
using ImageResizer.Plugins;
using ImageResizerGUI.Code;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using ImageResizer;
using ImageResizer.Plugins.BatchZipper;
using ImageResizer.Plugins.AnimatedGifs;
using ImageResizer.Plugins.PrettyGifs;
using ImageResizer.Plugins.PsdReader;
using System.Collections.ObjectModel;
using DataFormats = System.Windows.DataFormats;
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

        private string saveZipPath;

        private BackgroundWorker bwResizeBatch;

        private BackgroundWorker bwResizeBatchAndZip;

        AdvancedOptions aOptions;

        private SaveMode saveMode;

        private List<string> failedItems;

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            
            // Install Plugins
            new AnimatedGifs().Install(Config.Current);
            new PrettyGifs().Install(Config.Current);
            new PsdReader().Install(Config.Current);



            aOptions = new AdvancedOptions(this);

            btn_BrowseButton.Click += btn_BrowseButton_Click;
            btn_addFiles.Click += btn_addFiles_Click;
            btn_remove.Click += btn_remove_Click;
            btn_clearAll.Click += btn_clearAll_Click;
            tbox_height.PreviewTextInput += tbox_PreviewTextInput;
            tbox_width.PreviewTextInput += tbox_PreviewTextInput;
            Closing += MainWindow_Closing;
            listView.Drop += listView_Drop;
            KeyDown += MainWindow_KeyDown;

            comboBox_exportAction.SelectionChanged += comboBox_exportAction_SelectionChanged;
            comboBox_exportAction.SelectedIndex = 1;
            saveMode = SaveMode.ExportResults;
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
                int width = int.Parse(tbox_width.Text);

                switch (aOptions.cbox_resizeMode.SelectedIndex)
                {
                    case 0: //Shrink
                        rs.MaxHeight = height;
                        rs.MaxWidth = width;
                        break;
                    case 1: //Shrink and pad to ratio
                        rs.Height = height;
                        rs.Width = width;
                        break;
                    case 2: //Shrink and crop to ratio
                        rs.Height = height;
                        rs.Width = width;
                        rs.CropMode = CropMode.Auto;
                        break;
                }

                return rs;
            }
        }

        #region Events

        void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.V)
                if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                {
                    var returnList = System.Windows.Clipboard.GetFileDropList();

                    foreach (var item in returnList)
                    {
                        if (Directory.Exists(item))
                            AddDirectoryToBatch(item);

                        if (File.Exists(item))
                            AddFileToBatch(item);
                    }
                }
        }

        void listView_Drop(object sender, System.Windows.DragEventArgs e)
        {
            string[] filePaths = (string[])(e.Data.GetData(DataFormats.FileDrop));

            foreach (string fileLoc in filePaths)
            {
                // Code to read the contents of the text file
                if (File.Exists(fileLoc))
                {
                    AddFileToBatch(fileLoc);
                }

                if (Directory.Exists(fileLoc))
                {
                    AddDirectoryToBatch(fileLoc);
                }
            }
        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            Properties.Settings.Default.Save();

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
                    dialog.RootFolder = Environment.SpecialFolder.MyComputer;
                    dialog.SelectedPath = Properties.Settings.Default.saveFolderPath ?? Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);
                    dialog.ShowNewFolderButton = true;

                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        Properties.Settings.Default.saveFolderPath = tbox_savePath.Text = dialog.SelectedPath;
                }
            }

            if (saveMode == SaveMode.CreateZipFile)
            {
                using (var dialog = new SaveFileDialog())
                {
                    dialog.InitialDirectory = Properties.Settings.Default.saveFolderPath ?? Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);
                    dialog.Filter = "Zip File(*.ZIP)|*.ZIP";

                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        BatchInfo saveFile = new BatchInfo(dialog.FileName);
                        saveZipPath = tbox_savePath.Text = dialog.FileName;
                        Properties.Settings.Default.saveFolderPath = saveFile.Folder;
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
                    tbox_savePath.Text = Properties.Settings.Default.saveFolderPath;
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
                var openFileDialog = new OpenFileDialog { Multiselect = true };

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

                if (!string.IsNullOrEmpty(Properties.Settings.Default.openFolderPath))
                    openFileDialog.InitialDirectory = Properties.Settings.Default.openFolderPath;

                if (openFileDialog.ShowDialog(this) == true)
                {
                    if (openFileDialog.FileNames.Length > 0)
                    {
                        BatchInfo openFile = new BatchInfo(openFileDialog.FileName);
                        Properties.Settings.Default.openFolderPath = openFile.Folder;

                        AddFilesToBatch(openFileDialog.FileNames);
                    }
                }

            }
            catch
            { }
        }

        void btn_resize_Click(object sender, RoutedEventArgs e)
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
                    if (File.Exists(saveZipPath))
                        File.Delete(saveZipPath);
                    ResizeBatchAndZip();
                    break;
            }


        }

        void btn_showResults_Click(object sender, RoutedEventArgs e)
        {
            gridResults.Visibility = Visibility.Visible;
        }

        void Button_Click_1(object sender, RoutedEventArgs e)
        {
            gridResults.Visibility = Visibility.Collapsed;
            btn_showResults.Visibility = Visibility.Visible;
        }

        void btnCancel_Click(object sender, RoutedEventArgs e)
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

        void Button_Click_2(object sender, RoutedEventArgs e)
        {
            aOptions.SetData(int.Parse(tbox_height.Text), int.Parse(tbox_width.Text));
            aOptions.ShowDialog();
        }

        void btn_addFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Add image files recursively";
                dialog.ShowNewFolderButton = true;
                dialog.RootFolder = Environment.SpecialFolder.MyComputer;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    AddDirectoryToBatch(dialog.SelectedPath);
            }

        }

        void btn_viewResults_Click(object sender, RoutedEventArgs e)
        {
            string path = (string.IsNullOrEmpty(Properties.Settings.Default.saveFolderPath)) ? @"c:\" : Properties.Settings.Default.saveFolderPath;
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
                ((BatchInfo)item).StatusText = "Waiting";
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

        void AddFilesToBatch(IEnumerable<string> files)
        {
            foreach (var item in files)
                AddFileToBatch(item);
        }

        bool AddFileToBatch(string file)
        {
            if (!File.Exists(file))
            {
                MessageBox.Show("The file: " + file + " doesn't exist.");
                return false;
            }

            var extensions = ImageBuilder.Current.GetSupportedFileExtensions();

            foreach (var ext in extensions)
                if (file.ToLower().EndsWith("." + ext.ToLower()))
                {
                    if (!ImageInserted(file))
                        listView.Items.Add(new BatchInfo(file));
                    else
                        MessageBox.Show("The file: " + file + " is already inserted.", "Duplicated Items");
                    return true;
                }

            MessageBox.Show(file + " is not an image file.", "Supported Items");

            return false;
        }

        void AddDirectoryToBatch(string directory)
        {
            var files = FileTools.GetFilesRecursive(directory);

            foreach (var fileName in files)
                AddFileToBatch(fileName);
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

            bwResizeBatchAndZip.ReportProgress((e.Stats.SuccessfulItems * 100) / e.Stats.RequestedItems, e.Result);
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
                failedItems = new List<string>();

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

        #endregion

    }
}
