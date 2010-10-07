using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.IO;
using System.Windows.Forms;

using PhotoshopFile;

namespace PhotoShopTest
{
  public partial class MainForm : Form
  {
    Properties.Settings m_settings = new PhotoShopTest.Properties.Settings();

    public MainForm()
    {
      InitializeComponent();
    }

    private void OnCloseClick(object sender, EventArgs e)
    {
      Close();
    }
    private void OnBrowseClick(object sender, EventArgs e)
    {
      m_openFileDialog.FileName = m_fileName.Text;
      if (m_openFileDialog.ShowDialog(this) == DialogResult.OK)
      {
        m_fileName.Text = m_openFileDialog.FileName;
        Application.DoEvents();

        OnOpenClick(this, EventArgs.Empty);
      }
    }

    private void OnOpenClick(object sender, EventArgs e)
    {
      string fileName = m_fileName.Text;
      if (File.Exists(fileName))
      {
        UpdateFileLst(fileName);

        PsdFile pt = new PsdFile();

        this.Cursor = Cursors.WaitCursor;
        pt.Load(fileName);

        m_fileStructure.Nodes.Clear();

        TreeNode fileNode = new TreeNode("PSD File");
        fileNode.Tag = pt;

        m_fileStructure.Nodes.Add(fileNode);

        foreach (Layer layer in pt.Layers)
        {
          TreeNode layerNode = new TreeNode("Layer Name=" + layer.Name);
          layerNode.Tag = layer;
          fileNode.Nodes.Add(layerNode);

          foreach (Layer.Channel ch in layer.Channels)
          {
            TreeNode chNode = new TreeNode("Channel ID=" + ch.ID.ToString());
            chNode.Tag = ch;
            layerNode.Nodes.Add(chNode);
          }

          TreeNode maskNode = new TreeNode("Mask");
          maskNode.Tag = layer.MaskData;
          layerNode.Nodes.Add(maskNode);

          TreeNode blendingNode = new TreeNode("BlendingRangesData");
          blendingNode.Tag = layer.BlendingRangesData;
          layerNode.Nodes.Add(blendingNode);

          foreach (Layer.AdjustmentLayerInfo adjInfo in layer.AdjustmentInfo)
          {
            TreeNode node = new TreeNode(adjInfo.Key);
            node.Tag = adjInfo;
            layerNode.Nodes.Add(node);
          }
        }

        m_fileStructure.SelectedNode = fileNode;

        this.Cursor = Cursors.Default;

        //pt.Save(Path.Combine(Path.GetDirectoryName(m_fileName.Text), Path.GetFileNameWithoutExtension(m_fileName.Text) + "-s.psd"));
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////

    void UpdateFileLst(string fileName)
    {

      if (fileName.Length == 0)
        return;

      //---------------------------------------------------------------------------------

      if (m_fileName.Items.Contains(fileName))
      {
        int idx = m_fileName.Items.IndexOf(fileName);
        m_fileName.Items.RemoveAt(idx);
      }

      //---------------------------------------------------------------------------------

      m_fileName.Items.Insert(0, fileName);

      //---------------------------------------------------------------------------------

      while (m_fileName.Items.Count > 10)
      {
        m_fileName.Items.RemoveAt(m_fileName.Items.Count - 1);
      }
    }


    private void m_fileStructure_AfterSelect(object sender, TreeViewEventArgs e)
    {
      m_properties.SelectedObject = e.Node.Tag;

      if (e.Node.Tag is PsdFile)
      {
        pictureBox1.Image = ImageDecoder.DecodeImage(e.Node.Tag as PsdFile);
      }
      else if (e.Node.Tag is Layer)
      {
        pictureBox1.Image = ImageDecoder.DecodeImage(e.Node.Tag as Layer);
      }
      else if (e.Node.Tag is Layer.Mask)
      {
        pictureBox1.Image = ImageDecoder.DecodeImage(e.Node.Tag as Layer.Mask);
      }
    }

    private void OnSaveClick(object sender, EventArgs e)
    {
      if (m_fileStructure.Nodes.Count == 0)
        return;

      PsdFile psdFileSrc = (PsdFile)m_fileStructure.Nodes[0].Tag;


      PsdFile psdFile = new PsdFile();

      //-----------------------------------------------------------------------

      psdFile.Rows = psdFileSrc.Rows;
      psdFile.Columns = psdFileSrc.Columns;

      // we have an Alpha channel which will be saved, 
      // we have to add this to our image resources
      psdFile.Channels = 3;// 4;

      // for now we oly save the images as RGB
      psdFile.ColorMode = PsdFile.ColorModes.RGB;

      psdFile.Depth = 8;

      //-----------------------------------------------------------------------
      // no color mode Data

      //-----------------------------------------------------------------------

      psdFile.ImageResources.Clear();
      psdFile.ImageResources.AddRange(psdFileSrc.ImageResources.ToArray());

      //-----------------------------------------------------------------------

      int size = psdFile.Rows * psdFile.Columns;

      psdFile.ImageData = new byte[psdFile.Channels][];
      for (int i = 0; i < psdFile.Channels; i++)
      {
        psdFile.ImageData[i] = new byte[size];
      }

      Bitmap bmp = ImageDecoder.DecodeImage(psdFileSrc);

      for (int y = 0; y < psdFile.Rows; y++)
      {
        int rowIndex = y * psdFile.Columns;

        for (int x = 0; x < psdFile.Columns; x++)
        {
          int pos = rowIndex + x;

          Color pixelColor = bmp.GetPixel(x, y);

          psdFile.ImageData[0][pos] = pixelColor.R;
          psdFile.ImageData[1][pos] = pixelColor.G;
          psdFile.ImageData[2][pos] = pixelColor.B;
          //psdFile.ImageData[3][pos] = pixelColor.A;
        }
      }

      //-----------------------------------------------------------------------

      psdFile.ImageCompression = ImageCompression.Rle;

      psdFile.Save(Path.Combine(Path.GetDirectoryName(m_fileName.Text), Path.GetFileNameWithoutExtension(m_fileName.Text) + "-saved.psd"));
    }

    private void OnLoad(object sender, EventArgs e)
    {
      m_fileName.Items.Clear();

      foreach (string s in m_settings.MruFiles)
      {
        m_fileName.Items.Add(s);
      }

      if (m_fileName.Items.Count > 0)
        m_fileName.SelectedIndex = 0;
    }

    private void OnClosed(object sender, FormClosedEventArgs e)
    {
      m_settings.MruFiles.Clear();
      foreach (string s in m_fileName.Items)
      {
        m_settings.MruFiles.Add(s);
      }

      m_settings.Save();
    }

  }
}