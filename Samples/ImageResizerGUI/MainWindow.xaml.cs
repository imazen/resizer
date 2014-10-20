using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using ImageResizer.Configuration;
using ImageResizer.Plugins.BatchZipper;
using ImageResizerGUI.Code;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using ImageResizer;
using ImageResizer.Plugins.AnimatedGifs;
using ImageResizer.Plugins.PrettyGifs;
using System.Collections.ObjectModel;
using Control = System.Windows.Forms.Control;
using DataFormats = System.Windows.DataFormats;
using TextBox = System.Windows.Controls.TextBox;
using System.Windows.Threading;
using System.Security.Permissions;

namespace ImageResizerGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        delegate void AfterSettingChangedOnMainWindowsEventHandler();
        event AfterSettingChangedOnMainWindowsEventHandler AfterSettingChangedOnMainWindows;

        AdvancedOptions aOptions;

        private SaveMode saveMode;

        private BatchBackgroundWorking batchBackgroundWorking;

        private ZipBackgroundWorking zipBackgroundWorking;

        private bool cancelled;

        private int count;

        private string lastUsedPath;

        public MainWindow()
        {
            InitializeComponent();

            // Install Plugins
            new AnimatedGifs().Install(Config.Current);
            new PrettyGifs().Install(Config.Current);
            
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
            tbox_savePath.TextChanged += tbox_savePath_TextChanged;

            LoadSettings();

            aOptions.SetData(int.Parse(tbox_height.Text), int.Parse(tbox_width.Text));

            AfterSettingChangedOnMainWindows += MainWindow_AfterSettingChangedOnMainWindows;
            tbox_height.TextChanged += tbox_TextChanged;
            tbox_width.TextChanged += tbox_TextChanged;

            if (tbox_savePath.Text == "")
                tbox_savePath.Background = new SolidColorBrush(Colors.Pink);
        }


        /// <summary>
        /// Load initial data from last used settings
        /// </summary>
        void LoadSettings()
        {
            // if settings are not saved, create default settings
            if (string.IsNullOrEmpty(Properties.Settings.Default.querystring))
            {
                Properties.Settings.Default.querystring = "maxwidth=1024&maxheight=768";
                Properties.Settings.Default.Save();
            }


            ResizeSettings rs = new ResizeSettings(Properties.Settings.Default.querystring);

            // set settings to the GUI
            tbox_width.Text = (rs.Width != -1) ? rs.Width.ToString() : rs.MaxWidth.ToString();
            tbox_height.Text = (rs.Height != -1) ? rs.Height.ToString() : rs.MaxHeight.ToString();

            if (Properties.Settings.Default.querystring.Contains("maxheight"))
                aOptions.cbox_resizeMode.SelectedIndex = 0;
            else if (Properties.Settings.Default.querystring.Contains("crop=auto"))
                aOptions.cbox_resizeMode.SelectedIndex = 2;
            else
                aOptions.cbox_resizeMode.SelectedIndex = 1;

            if (Properties.Settings.Default.querystring.Contains("scale=both"))
                aOptions.cbUpscale.IsChecked = true;

            if (!string.IsNullOrEmpty(Properties.Settings.Default.saveFolderPath))
                tbox_savePath.Text = Properties.Settings.Default.saveFolderPath;
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

                switch ((ResizeMode)((ComboBoxItem)aOptions.cbox_resizeMode.SelectedItem).Tag)
                {
                    case ImageResizerGUI.ResizeMode.Shrink:
                        rs.MaxHeight = height;
                        rs.MaxWidth = width;
                        rs.Mode = FitMode.Max;
                        break;

                    case ImageResizerGUI.ResizeMode.ShrinkAndPadToRatio:
                        rs.Height = height;
                        rs.Width = width;
                        rs.Mode = FitMode.Pad;
                        break;

                    case ImageResizerGUI.ResizeMode.ShrinkAndCropToRatio:
                        rs.Height = height;
                        rs.Width = width;
                        rs.Mode = FitMode.Crop;
                        break;
                }

                return rs;
            }
        }

        void tbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                int.Parse(tbox_height.Text);
                int.Parse(tbox_width.Text);
            }
            catch (Exception)
            {
                e.Handled = true;
                ((TextBox)sender).Text = 1.ToString();
                ((TextBox)sender).Background = new SolidColorBrush(Colors.Pink);
                ((TextBox)sender).SelectAll();
            }
            if (AfterSettingChangedOnMainWindows != null)
                AfterSettingChangedOnMainWindows();
        }

        void MainWindow_AfterSettingChangedOnMainWindows()
        {
            try
            {
                int.Parse(tbox_height.Text);
                int.Parse(tbox_width.Text);

                if (tbox_height.Text != aOptions.tbox_maxHeight.Text || tbox_width.Text != aOptions.tbox_maxWidth.Text)
                    aOptions.SetData(int.Parse(tbox_height.Text), int.Parse(tbox_width.Text));

                Properties.Settings.Default.querystring = aOptions.QueryString;
                Properties.Settings.Default.Save();
            }
            catch (Exception)
            {
            }
        }

        void tbox_savePath_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (saveMode == SaveMode.ExportResults)
            {
                if (Directory.Exists(tbox_savePath.Text))
                {
                    tbox_savePath.Background = new SolidColorBrush(Colors.White);
                    Properties.Settings.Default.saveFolderPath = tbox_savePath.Text;
                    Properties.Settings.Default.Save();
                }

                else
                    tbox_savePath.Background = new SolidColorBrush(Colors.Pink);

            }

            if (saveMode == SaveMode.CreateZipFile)
            {
                BatchInfo saveFile = new BatchInfo(tbox_savePath.Text);

                if (Directory.Exists(saveFile.Folder) && tbox_savePath.Text.ToLower().EndsWith(".zip"))
                {
                    tbox_savePath.Background = new SolidColorBrush(Colors.White);
                    Properties.Settings.Default.saveZipPath = tbox_savePath.Text;
                    Properties.Settings.Default.Save();
                }
                else
                    tbox_savePath.Background = new SolidColorBrush(Colors.Pink);
            }
        }

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
            if (!btn_viewResults.IsEnabled)
            {
                if (System.Windows.MessageBox.Show("Do you want to interrupt the current work?", "Program Close", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    if (batchBackgroundWorking != null)
                        batchBackgroundWorking.StopWork();

                    if (zipBackgroundWorking != null)
                        zipBackgroundWorking.StopWork();
                }
                else
                    e.Cancel = true;
            }
            aOptions.Close();
        }

        void tbox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            try
            {
                Convert.ToInt32(((TextBox)sender).Text + e.Text);

                ((TextBox)sender).Background = new SolidColorBrush(Colors.White);
            }
            catch
            {
                e.Handled = true;
                ((TextBox)sender).Text = 1.ToString();
                ((TextBox)sender).Background = new SolidColorBrush(Colors.Pink);
                ((TextBox)sender).SelectAll();
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
                    dialog.Filter = "Zip File(*.zip)|*.zip";

                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        BatchInfo saveFile = new BatchInfo(dialog.FileName);
                        Properties.Settings.Default.saveZipPath = tbox_savePath.Text = dialog.FileName;
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
                    tbox_savePath.Text = Properties.Settings.Default.saveZipPath;
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
                    lastUsedPath = Properties.Settings.Default.saveFolderPath;
                    ResizeBatch();
                    break;

                case SaveMode.CreateZipFile:
                    if (File.Exists(Properties.Settings.Default.saveZipPath))
                        File.Delete(Properties.Settings.Default.saveZipPath);
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
            if (batchBackgroundWorking != null)
                batchBackgroundWorking.StopWork();

            // Turn off from here the progress bar and cancel button and report that.
            tbStatus.Text = "Status : Canceled";
            cancelled = true;

            btnCancel.Visibility = pBar1.Visibility = Visibility.Hidden;

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
            if (!string.IsNullOrEmpty(lastUsedPath) && Directory.Exists(lastUsedPath))
            {
                //string path = (string.IsNullOrEmpty(Properties.Settings.Default.saveFolderPath)) ? @"c:\" : Properties.Settings.Default.saveFolderPath;
                System.Diagnostics.Process.Start(lastUsedPath);
            }

        }


        void ResizeBatch()
        {
            var resultItems = new ObservableCollection<BatchInfo>();
            var items = new List<BatchInfo>();

            foreach (var item in listView.Items)
            {
                ((BatchInfo)item).StatusText = "Waiting";
                ((BatchInfo)item).Status = 0;
                resultItems.Add(((BatchInfo)item));
                items.Add(((BatchInfo)item));
            }

            dataGridResults.ItemsSource = resultItems;

            batchBackgroundWorking = new BatchBackgroundWorking(aOptions.QueryString, items, saveMode);

            batchBackgroundWorking.DoWorkEvent += batchBackgroundWorking_DoWorkEvent;
            batchBackgroundWorking.ProgressChangedEvent += batchBackgroundWorking_ProgressChangedEvent;
            batchBackgroundWorking.RunWorkerCompletedEvent += batchBackgroundWorking_RunWorkerCompletedEvent;

            batchBackgroundWorking.InitWork();

        }

        void ResizeBatchAndZip()
        {
            var resultItems = new ObservableCollection<BatchInfo>();
            var items = new List<BatchInfo>();

            foreach (var item in listView.Items)
            {
                ((BatchInfo)item).StatusText = "Waiting";
                ((BatchInfo)item).Status = 0;
                resultItems.Add(((BatchInfo)item));
                items.Add(((BatchInfo)item));
            }

            dataGridResults.ItemsSource = resultItems;

            zipBackgroundWorking = new ZipBackgroundWorking(aOptions.QueryString, items, saveMode);

            zipBackgroundWorking.DoWorkEvent += zipBackgroundWorking_DoWorkEvent;
            zipBackgroundWorking.ProgressChangedEvent += zipBackgroundWorking_ProgressChangedEvent;
            zipBackgroundWorking.RunWorkerCompletedEvent += zipBackgroundWorking_RunWorkerCompletedEvent;

            zipBackgroundWorking.InitWork();

        }


        /// <summary>
        /// Check in and image is arready inserted.
        /// </summary>
        /// <param name="imageFullPath"> Image path. </param>
        /// <returns> Returns true if the image is already inserted, false i.o.c. </returns>
        bool ImageInserted(string imageFullPath)
        {
            var items = from object item in listView.Items
                        where ((BatchInfo)item).FullPath == imageFullPath
                        select item;

            if (items.Count() > 0)
                return true;
            return false;
        }

        /// <summary>
        /// Check if file and folder configurations are OK.
        /// </summary>
        bool CheckFileSettings()
        {
            if (listView.Items.Count == 0)
            {
                System.Windows.MessageBox.Show("You must add a file.");
                return false;
            }

            if (saveMode == SaveMode.ExportResults)
            {
                if (!Directory.Exists(Properties.Settings.Default.saveFolderPath) || !Directory.Exists(tbox_savePath.Text))
                {
                    System.Windows.MessageBox.Show("The selected folder does not exist. Select a correct folder.");
                    return false;
                }
            }

            if (saveMode == SaveMode.CreateZipFile)
            {
                var saveFile = new BatchInfo(tbox_savePath.Text);

                if (!(Directory.Exists(saveFile.Folder) && tbox_savePath.Text.ToLower().EndsWith(".zip")))
                {
                    System.Windows.MessageBox.Show("You must select a valid destination ZIP file.");
                    return false;
                }


                if (string.IsNullOrEmpty(Properties.Settings.Default.saveZipPath))
                {
                    System.Windows.MessageBox.Show("You must select a destination ZIP file.");
                    return false;
                }
                var bInfo = new BatchInfo(Properties.Settings.Default.saveZipPath);

                if (!Directory.Exists(bInfo.Folder))
                {
                    System.Windows.MessageBox.Show("The selected folder does not exist. Select a correct folder.");
                    return false;
                }
            }
            return true;
        }


        string AddFilesToBatch(IEnumerable<string> files)
        {
            var error = "";

            foreach (var item in files)
                error += AddFileToBatch(item) + "; ";

            return error;
        }

        string AddFileToBatch(string file)
        {
            var error = "";

            if (!File.Exists(file))
            {
                error += "The file: " + file + " doesn't exist.";
                return error;
            }

            var extensions = ImageBuilder.Current.GetSupportedFileExtensions();

            foreach (var ext in extensions)
                if (file.ToLower().EndsWith("." + ext.ToLower()))
                {
                    if (!ImageInserted(file))
                    {
                        listView.Items.Add(new BatchInfo(file));
                        return "";
                    }
                    return "The file: " + file + " is already inserted.";
                }

            return file + " is not an image file.";
        }

        void AddDirectoryToBatch(string directory)
        {
            var files = FileTools.GetFilesRecursive(directory);

            foreach (var fileName in files)
                AddFileToBatch(fileName);
        }
    }
}
