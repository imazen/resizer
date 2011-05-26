using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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

            // avoid non-numerical data
            tbox_maxHeight.PreviewTextInput += tbox_PreviewTextInput;
            tbox_maxWidth.PreviewTextInput += tbox_PreviewTextInput;
            cbox_resizeMode.SelectionChanged += cbox_resizeMode_SelectionChanged;

            Loaded += AdvancedOptions_Loaded;
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
                Convert.ToInt32(e.Text);
            }
            catch
            {
                e.Handled = true;
            }
        }

        public void SetData(int height, int width)
        {
            tbox_maxHeight.Text = height.ToString();
            tbox_maxWidth.Text = width.ToString();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            parent.tbox_height.Text = tbox_maxHeight.Text;
            parent.tbox_width.Text = tbox_maxWidth.Text;
            this.Hide();
        }

        public string QueryString
        {
            get
            {
                switch (cbox_resizeMode.SelectedIndex)
                {
                    case 0: //Shrink
                        return "maxwidth=" + tbox_maxWidth.Text + "&maxheight=" + tbox_maxHeight.Text;
                    case 1: //Shrink and pad to ratio
                        return "width=" + tbox_maxWidth.Text + "&height=" + tbox_maxHeight.Text;
                    case 2: //Shrink and crop to ratio
                        return "width=" + tbox_maxWidth.Text + "&height=" + tbox_maxHeight.Text + "&crop=auto";
                } 
                return "error";
            }
        }
    }
}
