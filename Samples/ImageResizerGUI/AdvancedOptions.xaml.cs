using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ImageResizerGUI
{
    /// <summary>
    /// Interaction logic for AdvancedOptions.xaml
    /// </summary>
    public partial class AdvancedOptions : Window
    {
        MainWindow parent;

        public AdvancedOptions(MainWindow parent)
        {
            InitializeComponent();

            this.parent = parent;

            cbox_resizeMode.Items.Add(new ComboBoxItem() { Content = "Shrink", Tag = ImageResizerGUI.ResizeMode.Shrink });
            cbox_resizeMode.Items.Add(new ComboBoxItem() { Content = "ShrinkAndCropToRatio", Tag = ImageResizerGUI.ResizeMode.ShrinkAndCropToRatio });
            cbox_resizeMode.Items.Add(new ComboBoxItem() { Content = "ShrinkAndPadToRatio", Tag = ImageResizerGUI.ResizeMode.ShrinkAndPadToRatio });

            // avoid non-numerical data
            tbox_maxHeight.PreviewTextInput += tbox_PreviewTextInput;
            tbox_maxWidth.PreviewTextInput += tbox_PreviewTextInput;

            tbox_maxHeight.TextChanged += tbox_TextChanged;
            tbox_maxWidth.TextChanged += tbox_TextChanged;

            cbox_resizeMode.SelectionChanged += cbox_resizeMode_SelectionChanged;

            btn_ok.Click += btn_ok_Click;
            btn_cancel.Click += btn_cancel_Click;

            Loaded += AdvancedOptions_Loaded;

            cbUpscale.Checked += cbUpscale_Checked;
            cbUpscale.Unchecked += cbUpscale_Unchecked;
        }

        void cbUpscale_Unchecked(object sender, RoutedEventArgs e)
        {
            tbox_query.Text = QueryString;
        }

        void cbUpscale_Checked(object sender, RoutedEventArgs e)
        {
            tbox_query.Text = QueryString;
        }

        void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetData(int.Parse(parent.tbox_height.Text), int.Parse(parent.tbox_width.Text));
            }
            catch (Exception) { }

            Hide();
        }

        void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            parent.tbox_height.Text = tbox_maxHeight.Text;
            parent.tbox_width.Text = tbox_maxWidth.Text;

            Properties.Settings.Default.querystring = QueryString;
            Properties.Settings.Default.Save();

            Hide();
        }

        void AdvancedOptions_Loaded(object sender, RoutedEventArgs e)
        {
            tbox_query.Text = QueryString;
        }

        void cbox_resizeMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            tbox_query.Text = QueryString;
        }

        void tbox_PreviewTextInput(object sender, TextCompositionEventArgs e)
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

        void tbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                int.Parse(((TextBox)sender).Text);
                ((TextBox)sender).Background = new SolidColorBrush(Colors.White);
            }
            catch (Exception)
            {
                ((TextBox)sender).Text = 1.ToString();
                ((TextBox)sender).Background = new SolidColorBrush(Colors.Pink);
                ((TextBox)sender).SelectAll();
            }
            tbox_query.Text = QueryString;
        }

        public void SetData(int height, int width)
        {
            tbox_maxHeight.Text = height.ToString();
            tbox_maxWidth.Text = width.ToString();
        }

        public string QueryString
        {
            get
            {
                switch ((ResizeMode)((ComboBoxItem)cbox_resizeMode.SelectedItem).Tag)
                {
                    case ImageResizerGUI.ResizeMode.Shrink:
                        return "maxwidth=" + tbox_maxWidth.Text + "&maxheight=" + tbox_maxHeight.Text + ((cbUpscale.IsChecked == true) ? "&scale=both" : "");

                    case ImageResizerGUI.ResizeMode.ShrinkAndPadToRatio:
                        return "width=" + tbox_maxWidth.Text + "&height=" + tbox_maxHeight.Text + ((cbUpscale.IsChecked == true) ? "&scale=both" : "");

                    case ImageResizerGUI.ResizeMode.ShrinkAndCropToRatio:
                        return "width=" + tbox_maxWidth.Text + "&height=" + tbox_maxHeight.Text + "&crop=auto" + ((cbUpscale.IsChecked == true) ? "&scale=both" : "");
                }
                return "error";
            }
        }

    }
}
