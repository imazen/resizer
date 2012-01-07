using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace GifUI
{
    public partial class MergeFrm : Form
    {
        public int MergeType = 0;
        public MergeFrm()
        {
            InitializeComponent();
        }
        public List<string> SourceFiles = new List<string>();
        private void panel1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] gifs = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string g in gifs)
                {
                    if (Path.GetExtension(g).ToLower() != ".gif")
                    {
                        continue;
                    }
                    if (!SourceFiles.Contains(g))
                    {
                        SourceFiles.Add(g);
                        PictureBox p = new PictureBox();
                        p.ImageLocation = g;
                        p.Location = GetPos();
                        p.SizeMode = PictureBoxSizeMode.AutoSize;
                        panel1.Controls.Add(p);
                    }
                }                
            }
        }

        Point GetPos()
        {
            if (panel1.Controls.Count == 0)
            {
                return new Point(5, 5);
            }
            return new Point(panel1.Controls[panel1.Controls.Count - 1].Right + 5, 5);
        }

        private void panel1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Move;
            }
        }    

        private void button2_Click(object sender, EventArgs e)
        {
            this.SourceFiles.Clear();
            panel1.Controls.Clear();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            if (radioButton2.Checked)
            {
                MergeType = 1;
            }
            this.Close();
        }    
    }
}
