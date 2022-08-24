namespace PhotoShopTest
{
  partial class MainForm
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      this.m_btnOpen = new System.Windows.Forms.Button();
      this.m_openFileDialog = new System.Windows.Forms.OpenFileDialog();
      this.m_btnBrowse = new System.Windows.Forms.Button();
      this.pictureBox1 = new System.Windows.Forms.PictureBox();
      this.m_fileStructure = new System.Windows.Forms.TreeView();
      this.m_properties = new System.Windows.Forms.PropertyGrid();
      this.m_btnSave = new System.Windows.Forms.Button();
      this.m_fileName = new System.Windows.Forms.ComboBox();
      this.panel1 = new System.Windows.Forms.Panel();
      this.m_btnClose = new System.Windows.Forms.Button();
      ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
      this.panel1.SuspendLayout();
      this.SuspendLayout();
      // 
      // m_btnOpen
      // 
      this.m_btnOpen.Location = new System.Drawing.Point(483, 11);
      this.m_btnOpen.Name = "m_btnOpen";
      this.m_btnOpen.Size = new System.Drawing.Size(55, 23);
      this.m_btnOpen.TabIndex = 0;
      this.m_btnOpen.Text = "Open";
      this.m_btnOpen.UseVisualStyleBackColor = true;
      this.m_btnOpen.Click += new System.EventHandler(this.OnOpenClick);
      // 
      // m_openFileDialog
      // 
      this.m_openFileDialog.DefaultExt = "psd";
      this.m_openFileDialog.Filter = "Photoshop Files|*.psd|All Files|*.*";
      // 
      // m_btnBrowse
      // 
      this.m_btnBrowse.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      this.m_btnBrowse.Location = new System.Drawing.Point(422, 12);
      this.m_btnBrowse.Name = "m_btnBrowse";
      this.m_btnBrowse.Size = new System.Drawing.Size(55, 23);
      this.m_btnBrowse.TabIndex = 0;
      this.m_btnBrowse.Text = "Browse...";
      this.m_btnBrowse.UseVisualStyleBackColor = true;
      this.m_btnBrowse.Click += new System.EventHandler(this.OnBrowseClick);
      // 
      // pictureBox1
      // 
      this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
      this.pictureBox1.Location = new System.Drawing.Point(0, 0);
      this.pictureBox1.Name = "pictureBox1";
      this.pictureBox1.Size = new System.Drawing.Size(202, 168);
      this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
      this.pictureBox1.TabIndex = 2;
      this.pictureBox1.TabStop = false;
      // 
      // m_fileStructure
      // 
      this.m_fileStructure.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)));
      this.m_fileStructure.Location = new System.Drawing.Point(12, 41);
      this.m_fileStructure.Margin = new System.Windows.Forms.Padding(2);
      this.m_fileStructure.Name = "m_fileStructure";
      this.m_fileStructure.Size = new System.Drawing.Size(171, 504);
      this.m_fileStructure.TabIndex = 3;
      this.m_fileStructure.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.m_fileStructure_AfterSelect);
      // 
      // m_properties
      // 
      this.m_properties.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)));
      this.m_properties.HelpVisible = false;
      this.m_properties.Location = new System.Drawing.Point(187, 41);
      this.m_properties.Margin = new System.Windows.Forms.Padding(2);
      this.m_properties.Name = "m_properties";
      this.m_properties.Size = new System.Drawing.Size(154, 505);
      this.m_properties.TabIndex = 4;
      // 
      // m_btnSave
      // 
      this.m_btnSave.Location = new System.Drawing.Point(544, 11);
      this.m_btnSave.Name = "m_btnSave";
      this.m_btnSave.Size = new System.Drawing.Size(55, 23);
      this.m_btnSave.TabIndex = 0;
      this.m_btnSave.Text = "Save";
      this.m_btnSave.UseVisualStyleBackColor = true;
      this.m_btnSave.Click += new System.EventHandler(this.OnSaveClick);
      // 
      // m_fileName
      // 
      this.m_fileName.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
      this.m_fileName.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
      this.m_fileName.FormattingEnabled = true;
      this.m_fileName.Location = new System.Drawing.Point(12, 13);
      this.m_fileName.Name = "m_fileName";
      this.m_fileName.Size = new System.Drawing.Size(403, 21);
      this.m_fileName.TabIndex = 5;
      this.m_fileName.Text = "c:\\Dokumente und Einstellungen\\fblumenb\\Eigene Dateien\\downloads\\africa.psd";
      // 
      // panel1
      // 
      this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.panel1.AutoScroll = true;
      this.panel1.Controls.Add(this.pictureBox1);
      this.panel1.Location = new System.Drawing.Point(345, 41);
      this.panel1.Margin = new System.Windows.Forms.Padding(2);
      this.panel1.Name = "panel1";
      this.panel1.Size = new System.Drawing.Size(375, 504);
      this.panel1.TabIndex = 6;
      // 
      // m_btnClose
      // 
      this.m_btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      this.m_btnClose.Location = new System.Drawing.Point(666, 11);
      this.m_btnClose.Name = "m_btnClose";
      this.m_btnClose.Size = new System.Drawing.Size(55, 23);
      this.m_btnClose.TabIndex = 0;
      this.m_btnClose.Text = "Close";
      this.m_btnClose.UseVisualStyleBackColor = true;
      this.m_btnClose.Click += new System.EventHandler(this.OnCloseClick);
      // 
      // MainForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.CancelButton = this.m_btnClose;
      this.ClientSize = new System.Drawing.Size(729, 560);
      this.Controls.Add(this.panel1);
      this.Controls.Add(this.m_fileName);
      this.Controls.Add(this.m_properties);
      this.Controls.Add(this.m_fileStructure);
      this.Controls.Add(this.m_btnBrowse);
      this.Controls.Add(this.m_btnClose);
      this.Controls.Add(this.m_btnSave);
      this.Controls.Add(this.m_btnOpen);
      this.Name = "MainForm";
      this.Text = "PSD File Structure";
      this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.OnClosed);
      this.Load += new System.EventHandler(this.OnLoad);
      ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
      this.panel1.ResumeLayout(false);
      this.panel1.PerformLayout();
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Button m_btnOpen;
    private System.Windows.Forms.OpenFileDialog m_openFileDialog;
    private System.Windows.Forms.Button m_btnBrowse;
    private System.Windows.Forms.PictureBox pictureBox1;
    private System.Windows.Forms.TreeView m_fileStructure;
    private System.Windows.Forms.PropertyGrid m_properties;
    private System.Windows.Forms.Button m_btnSave;
    private System.Windows.Forms.ComboBox m_fileName;
    private System.Windows.Forms.Panel panel1;
    private System.Windows.Forms.Button m_btnClose;
  }
}

