using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Jillzhang.GifUtility;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace GifUI
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {           
            SplashScreen splashScrenn = new SplashScreen();
            splashScrenn.ShowDialog();
            openFileDialog1.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            folderBrowserDialog1.SelectedPath = AppDomain.CurrentDomain.BaseDirectory;
            backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork);
            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
        }

        
        Stopwatch sw = new Stopwatch();
        string outGifPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "outputgif.gif");
        void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            string gifPath = openFileDialog1.FileName;
            sw.Reset();
            sw.Start();
            switch (e.Argument.ToString())
            {
                case "Monochrome":
                    {
                        GifHelper.Monochrome(pictureBox1.ImageLocation, outGifPath);
                        break;
                    }
                case "RotateLeft":
                    {
                        GifHelper.Rotate(pictureBox1.ImageLocation, RotateFlipType.Rotate90FlipXY, outGifPath);
                        break;
                    }
                case "RotateRight":
                    {
                        GifHelper.Rotate(pictureBox1.ImageLocation, RotateFlipType.Rotate270FlipXY, outGifPath);
                        break;
                    }
                case "FlipH":
                    {
                        GifHelper.Rotate(pictureBox1.ImageLocation, RotateFlipType.RotateNoneFlipX, outGifPath);
                        break;
                    }
                case "FlipV":
                    {
                        GifHelper.Rotate(pictureBox1.ImageLocation, RotateFlipType.RotateNoneFlipY, outGifPath);
                        break;
                    }
                case "Thum50%":
                    {
                        GifHelper.GetThumbnail(pictureBox1.ImageLocation, 0.5, outGifPath);
                        break;
                    }
                case "Thum30%":
                    {
                        GifHelper.GetThumbnail(pictureBox1.ImageLocation, 0.3, outGifPath);
                        break;
                    }
                case "Thum120%":
                    {
                        GifHelper.GetThumbnail(pictureBox1.ImageLocation, 1.2, outGifPath);
                        break;
                    }
                case "Thum150%":
                    {
                        GifHelper.GetThumbnail(pictureBox1.ImageLocation, 1.5, outGifPath);
                        break;
                    }
                case "WaterMark":
                    {
                        GifHelper.WaterMark(pictureBox1.ImageLocation, SizeMode.Large, wmText.Text, wmText.ForceColor, wmText.Font, StartX, StartY, outGifPath);
                        break;
                    }
                case "WaterMarkWithImage":
                    {
                        GifHelper.WaterMark(pictureBox1.ImageLocation, waterImg, StartX, StartY, outGifPath);
                        break;
                    }
                case "Corp":
                    {
                        GifHelper.Crop(pictureBox1.ImageLocation, new Rectangle((int)StartX,(int)StartY,(int)(EndX-StartX),(int)(EndY-StartY)), outGifPath);
                        break;
                    }
                case "Merge":
                    {
                        GifHelper.Merge(mf.SourceFiles, outGifPath);
                        openFileDialog1.FileName = outGifPath;
                        break;
                    }
                case "Merge1":
                    {
                        GifHelper.Merge(mf.SourceFiles, outGifPath,50,true);
                        openFileDialog1.FileName = outGifPath;
                        break;
                    }
            }
            sw.Stop();
        }

        private void 浏览ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult dr = openFileDialog1.ShowDialog();
            if (dr == DialogResult.OK)
            {
                pictureBox1.ImageLocation = openFileDialog1.FileName;
                toolStripButton2.Enabled = toolStripDropDownButton2.Enabled = toolStripButton4.Enabled = toolStripDropDownButton1.Enabled = toolStripButton6.Enabled = toolStripButton3.Enabled = true;
            }
        }

        private void 单色化ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            backgroundWorker1.RunWorkerAsync("Monochrome");
            toolStripButton4.Enabled = false;
            toolStripStatusLabel1.Text = "正在对图像进行单色化......";
        }

        void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            toolStripStatusLabel1.Text = "完成,本次操作耗时:" + sw.ElapsedMilliseconds + "ms";
            pictureBox1.ImageLocation = outGifPath;
            toolStripButton2.Enabled = toolStripButton3.Enabled=toolStripButton6.Enabled = toolStripDropDownButton2.Enabled = toolStripButton4.Enabled = toolStripDropDownButton1.Enabled = toolStripButton5.Enabled = true;
            pictureBox1.Cursor = Cursors.Arrow;
            pictureBox1.SizeMode = PictureBoxSizeMode.CenterImage;
            wantCorp = false;
        }

        private void 缩略ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            backgroundWorker2.DoWork += new DoWorkEventHandler(backgroundWorker2_DoWork);
            backgroundWorker2.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker2_RunWorkerCompleted);
            backgroundWorker2.RunWorkerAsync();
            toolStripStatusLabel1.Text = "正在对图像缩略......";
        }

        string thGif = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "thGif.gif");
        void backgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            toolStripStatusLabel1.Text = "完成";
            pictureBox1.ImageLocation = thGif;
        }

        void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            GifHelper.GetThumbnail(pictureBox1.ImageLocation, 0.5, thGif);

        }
        string dir;
        private void 合并ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult dr = folderBrowserDialog1.ShowDialog();
            if (dr == DialogResult.OK)
            {
                dir = folderBrowserDialog1.SelectedPath;
                outGifPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "merge.gif");
                BackgroundWorker bg = new BackgroundWorker();
                bg.DoWork += new DoWorkEventHandler(bg_DoWork);
                bg.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bg_RunWorkerCompleted);
                toolStripStatusLabel1.Text = "正在对图像进行合并......";
                bg.RunWorkerAsync();
            }

        }

        void bg_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            toolStripStatusLabel1.Text = "完成";
            pictureBox1.ImageLocation = outGifPath;
        }

        void bg_DoWork(object sender, DoWorkEventArgs e)
        {
            List<string> sources = GetGifFiles(dir);
            GifHelper.Merge(sources, outGifPath);
        }

        List<string> GetGifFiles(string dir)
        {
            List<string> list = new List<string>();
            DirectoryInfo di = new DirectoryInfo(dir);
            foreach (FileInfo fi in di.GetFiles("*.gif"))
            {
                list.Add(fi.FullName);
            }
            return list;
        }
        #region 旋转
        private void 向左旋转ToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            toolStripDropDownButton1.Enabled = false;
            backgroundWorker1.RunWorkerAsync("RotateLeft");
            toolStripStatusLabel1.Text = "正在对图像向左旋转......";
        }

        private void 向右旋转ToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            toolStripDropDownButton1.Enabled = false;
            backgroundWorker1.RunWorkerAsync("RotateRight");
            toolStripStatusLabel1.Text = "正在对图像向右旋转......";
        }

        private void 水平翻转ToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            toolStripDropDownButton1.Enabled = false;
            backgroundWorker1.RunWorkerAsync("FlipH");
            toolStripStatusLabel1.Text = "正在对图像水平翻转.....";
        }

        private void 垂直翻转ToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            toolStripDropDownButton1.Enabled = false;
            backgroundWorker1.RunWorkerAsync("FlipV");
            toolStripStatusLabel1.Text = "正在对图像垂直翻转.....";
        }
        #endregion

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            pictureBox1.ImageLocation = openFileDialog1.FileName;
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            toolStripDropDownButton2.Enabled = false;
            backgroundWorker1.RunWorkerAsync("Thum50%");
            toolStripStatusLabel1.Text = "正在对图像缩放.....";
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            toolStripDropDownButton2.Enabled = false;
            backgroundWorker1.RunWorkerAsync("Thum30%");
            toolStripStatusLabel1.Text = "正在对图像缩放.....";
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            toolStripDropDownButton2.Enabled = false;
            backgroundWorker1.RunWorkerAsync("Thum120%");
            toolStripStatusLabel1.Text = "正在对图像缩放.....";
        }

        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            toolStripDropDownButton2.Enabled = false;
            backgroundWorker1.RunWorkerAsync("Thum150%");
            toolStripStatusLabel1.Text = "正在对图像缩放.....";
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            this.pictureBox1.Cursor = Cursors.Cross;
            toolStripButton2.Enabled = false;
            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
        }
       
        float StartX = -1;
        float StartY = -1;
        WaterMarkText wmText;
        Bitmap waterImg;
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && this.pictureBox1.Cursor == Cursors.Cross && !wantCorp)
            {
                StartX = e.X;
                StartY = e.Y;    
                WaterMark wm = new WaterMark();
                wm.Left = this.PointToScreen(e.Location).X;
                wm.Top = this.PointToScreen(e.Location).Y;               
                DialogResult dr  =   wm.ShowDialog();
                if (dr == DialogResult.OK)
                {
                    if (wm.WaterImage == null)
                    {
                        wmText = wm.WaterMarkText;
                        toolStripButton2.Enabled = false;
                        backgroundWorker1.RunWorkerAsync("WaterMark");
                        toolStripStatusLabel1.Text = "正在对图像添加水印.....";
                    }
                    else
                    {
                        waterImg = wm.WaterImage;
                        toolStripButton2.Enabled = false;
                        backgroundWorker1.RunWorkerAsync("WaterMarkWithImage");
                        toolStripStatusLabel1.Text = "正在对图像添加图像水印.....";
                    }
                }
            }
            else if (e.Button == MouseButtons.Left && this.pictureBox1.Cursor == Cursors.Cross && wantCorp)
            {
                StartX = e.X;
                StartY = e.Y;                   
            }
        }
        bool wantCorp = false;
        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            this.pictureBox1.Cursor = Cursors.Cross;
            toolStripButton6.Enabled = false;
            wantCorp = true;
            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && this.pictureBox1.Cursor == Cursors.Cross && wantCorp)
            {
                Graphics g = pictureBox1.CreateGraphics();
                Pen p = new Pen(new SolidBrush(Color.Black), 1.2f);
                Rectangle rect = new Rectangle((int)StartX,(int)StartY,(int)(e.X-StartX),(int)(e.Y-StartY));
                g.DrawRectangle(p,rect);
                pictureBox1.Invalidate(rect);
            }
        }
        int EndX;
        int EndY;
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && this.pictureBox1.Cursor == Cursors.Cross && wantCorp)
            {            
                EndX = e.X;
                EndY = e.Y;                
                backgroundWorker1.RunWorkerAsync("Corp");
                toolStripStatusLabel1.Text = "正在对图像剪裁.....";
                wantCorp = false;
            }
        }
        MergeFrm mf;
        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            toolStripButton3.Enabled = false;
            mf = new MergeFrm();
            DialogResult dr =  mf.ShowDialog();
            if (dr == DialogResult.OK)
            {
                if (mf.MergeType == 1)
                {
                    backgroundWorker1.RunWorkerAsync("Merge");                   
                }
                else
                {
                    backgroundWorker1.RunWorkerAsync("Merge1");                   
                }
                toolStripStatusLabel1.Text = "正在对合成Gif图像.....";
            }
        }
    }
}