using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;

using PaintDotNet;

namespace PaintDotNet.Data.PhotoshopFileType
{
    /// <summary>
    /// Summary description for TgaSaveConfigWidget.
    /// </summary>
    public class PsdSaveConfigWidget 
        : PaintDotNet.SaveConfigWidget
    {
        private System.Windows.Forms.CheckBox m_rleCompressCheckBox;

        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        public PsdSaveConfigWidget()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

            //this.bpp24Radio.Text = PdnResources.GetString("TgaSaveConfigWidget.Bpp24Radio.Text");
            //this.bpp32Radio.Text = PdnResources.GetString("TgaSaveConfigWidget.Bpp32Radio.Text");
            //this.bppLabel.Text = PdnResources.GetString("TgaSaveConfigWidget.BppLabel.Text");
            //this.rleCompressCheckBox.Text = PdnResources.GetString("TgaSaveConfigWidget.RleCompressCheckBox.Text");
        }

        protected override void InitFileType()
        {
            this.fileType = new PhotoshopFileType();
        }

        protected override void InitTokenFromWidget()
        {
          ((PsdSaveConfigToken)this.token).RleCompress = this.m_rleCompressCheckBox.Checked;
        }

        protected override void InitWidgetFromToken(SaveConfigToken token)
        {
          if (token is PsdSaveConfigToken)
          {
            PsdSaveConfigToken psdToken = (PsdSaveConfigToken)token;
            this.m_rleCompressCheckBox.Checked = psdToken.RleCompress;
          }
          else
          {
            this.m_rleCompressCheckBox.Checked = true;
          }
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
          this.m_rleCompressCheckBox = new System.Windows.Forms.CheckBox();
          this.SuspendLayout();
          // 
          // m_rleCompressCheckBox
          // 
          this.m_rleCompressCheckBox.Location = new System.Drawing.Point(0, 0);
          this.m_rleCompressCheckBox.Name = "m_rleCompressCheckBox";
          this.m_rleCompressCheckBox.Size = new System.Drawing.Size(184, 24);
          this.m_rleCompressCheckBox.TabIndex = 0;
          this.m_rleCompressCheckBox.Text = "Compression (RLE)";
          this.m_rleCompressCheckBox.CheckedChanged += new System.EventHandler(this.OnCheckedChanged);
          // 
          // PsdSaveConfigWidget
          // 
          this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
          this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
          this.Controls.Add(this.m_rleCompressCheckBox);
          this.Name = "PsdSaveConfigWidget";
          this.Size = new System.Drawing.Size(180, 104);
          this.ResumeLayout(false);

        }
        #endregion

        private void OnCheckedChanged(object sender, System.EventArgs e)
        {
            this.UpdateToken();
        }
    }
}
