/*
* Copyright (c) 2006, Jonas Beckeman
* All rights reserved.
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions are met:
*
*     * Redistributions of source code must retain the above copyright
*       notice, this list of conditions and the following disclaimer.
*     * Redistributions in binary form must reproduce the above copyright
*       notice, this list of conditions and the following disclaimer in the
*       documentation and/or other materials provided with the distribution.
*     * Neither the name of Jonas Beckeman nor the names of its contributors
*       may be used to endorse or promote products derived from this software
*       without specific prior written permission.
*
* THIS SOFTWARE IS PROVIDED BY JONAS BECKEMAN AND CONTRIBUTORS ``AS IS'' AND ANY
* EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
* WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
* DISCLAIMED. IN NO EVENT SHALL JONAS BECKEMAN AND CONTRIBUTORS BE LIABLE FOR ANY
* DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
* (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
* LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
* ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
* (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
* SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*
* HEADER_END*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Endogine.Codecs.Photoshop;
using System.Xml;

namespace Psd
{
    public partial class Form1 : Form
    {
        Document _psd;
        string _draggedFile;
        XmlNode _xmlInfo;
        Endogine.Editors.TreeEditForm _xmlEditor;

        public Form1()
        {
            InitializeComponent();

            this.OpenXmlEditor();
            this._xmlEditor.Visible = false;
        }

        void OpenXmlEditor()
        {
            if (this._xmlEditor != null && !this._xmlEditor.IsDisposed)
                return;
            this._xmlEditor = new Endogine.Editors.TreeEditForm();
            this._xmlEditor.Show();
            this._xmlEditor.Size = new Size(600, 400);
            this._xmlEditor.TreeEdit.SplitterPosition = 200;
            this._xmlEditor.Location = new Point(10, 10);
            //this._xmlEditor.TopMost = true;
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            this._draggedFile = null;
            if (e.Data.GetDataPresent("FileName"))
            {
                string[] files = (string[])e.Data.GetData("FileName");
                if (files[0].ToLower().EndsWith(".psd"))
                {
                    e.Effect = DragDropEffects.Copy;
                    this._draggedFile = files[0];
                }
            }
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            if (this._draggedFile != null)
            {
                this.LoadFile(this._draggedFile);
            }
        }

        public void LoadFile(string filename)
        {
            string formerText = this.Text;
            this.Text = "Loading document";
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                this._psd = new Document(filename);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            this.button2.Enabled = true;

            this.Text = "Generating Xml";
            //This seems pretty stupid (can't the serializer write directly to a document?), but the best way I can find ATM...
            //System.IO.MemoryStream stream = new System.IO.MemoryStream();
            //System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(this._psd.GetType());
            //xs.Serialize(stream, this._psd);
            //this._xmlDoc.Load(stream);
            try
            {
                this._psd.SaveXml("_tmpSave.xml", false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load("_tmpSave.xml");

            //ResIdForTempXml

            this._xmlInfo = xmlDoc.SelectSingleNode("PsdDocument");
            Cursor.Current = Cursors.Default;
            //this.pictureBox1.Size = new Size(psd.Header.Columns, psd.Header.Rows);
            //XmlNode node = this._xmlInfo["Resources"];
            //foreach (XmlNode n in node.ChildNodes)

            this.comboBox1.Items.Clear();
            this.comboBox1.Items.Add("Global");
            foreach (Layer layer in this._psd.Layers)
                this.comboBox1.Items.Add(layer.Name);
            this.Text = "Filling Xml editor";
            this.comboBox1.SelectedIndex = 0;
            this.Text = formerText;

            this._xmlEditor.Visible = true;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this._psd == null)
                return;

            this.OpenXmlEditor();

            ComboBox cb = (ComboBox)sender;
            if (cb.SelectedIndex == 0)
            {
                if (this._psd.GlobalImage != null)
                    this.pictureBox1.Image = this._psd.GlobalImage.Bitmap;

                this._xmlEditor.TreeEdit.LoadXml(null, true);
                string xml = "";
                foreach (XmlNode node in this._xmlInfo.ChildNodes)
                {
                    if (node.Name != "Layers")
                    {
                        //xml += node.OuterXml + "\r\n";
                        this._xmlEditor.TreeEdit.LoadXml(node, false);
                    }
                }
                //this.textBox1.Text = xml;
            }
            else
            {
                Layer layer = this._psd.Layers[cb.SelectedIndex - 1];
                if (this._psd.GlobalImage != null)
                    this.pictureBox1.Image = layer.Bitmap;

                System.IO.MemoryStream stream = new System.IO.MemoryStream();
                //System.IO.TextWriter tw
                System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(layer.GetType());
                xs.Serialize(stream, layer);

                foreach (XmlNode node in this._xmlInfo.SelectSingleNode("Layers").ChildNodes)
                {
                    if (node.Attributes["Name"].InnerText == layer.Name)
                    {
                        //this.textBox1.Text = node.OuterXml;
                        this._xmlEditor.LoadXml(node);
                        break;
                    }
                }
            }

            this._xmlEditor.TreeEdit.ExpandAll();
            this._xmlEditor.TreeEdit.DataGridView.Columns[0].Width = 250;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.DefaultExt = ".xml";
            sfd.AddExtension = true;
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                this._psd.SaveXml(sfd.FileName, this.checkBox1.Checked);
            }
        }
    }
}