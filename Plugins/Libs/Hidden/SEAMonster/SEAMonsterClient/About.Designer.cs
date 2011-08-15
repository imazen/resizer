namespace SEAMonster
{
    partial class About
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(About));
            this.pctSEAMonsterLogo = new System.Windows.Forms.PictureBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.lnkSeamCarvingSite = new System.Windows.Forms.LinkLabel();
            this.lnkMichaelSwanson = new System.Windows.Forms.LinkLabel();
            ((System.ComponentModel.ISupportInitialize)(this.pctSEAMonsterLogo)).BeginInit();
            this.SuspendLayout();
            // 
            // pctSEAMonsterLogo
            // 
            this.pctSEAMonsterLogo.Image = ((System.Drawing.Image)(resources.GetObject("pctSEAMonsterLogo.Image")));
            this.pctSEAMonsterLogo.Location = new System.Drawing.Point(12, 12);
            this.pctSEAMonsterLogo.Name = "pctSEAMonsterLogo";
            this.pctSEAMonsterLogo.Size = new System.Drawing.Size(500, 145);
            this.pctSEAMonsterLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pctSEAMonsterLogo.TabIndex = 0;
            this.pctSEAMonsterLogo.TabStop = false;
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnOK.Location = new System.Drawing.Point(437, 197);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // lnkSeamCarvingSite
            // 
            this.lnkSeamCarvingSite.AutoSize = true;
            this.lnkSeamCarvingSite.LinkArea = new System.Windows.Forms.LinkArea(9, 45);
            this.lnkSeamCarvingSite.Location = new System.Drawing.Point(12, 177);
            this.lnkSeamCarvingSite.Name = "lnkSeamCarvingSite";
            this.lnkSeamCarvingSite.Size = new System.Drawing.Size(468, 17);
            this.lnkSeamCarvingSite.TabIndex = 2;
            this.lnkSeamCarvingSite.TabStop = true;
            this.lnkSeamCarvingSite.Text = "Based on Seam Carving for Content-Aware Image Resizing by Shai Avidan and Ariel S" +
                "hamir";
            this.lnkSeamCarvingSite.UseCompatibleTextRendering = true;
            this.lnkSeamCarvingSite.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkSeamCarvingSite_LinkClicked);
            // 
            // lnkMichaelSwanson
            // 
            this.lnkMichaelSwanson.AutoSize = true;
            this.lnkMichaelSwanson.LinkArea = new System.Windows.Forms.LinkArea(28, 15);
            this.lnkMichaelSwanson.Location = new System.Drawing.Point(13, 198);
            this.lnkMichaelSwanson.Name = "lnkMichaelSwanson";
            this.lnkMichaelSwanson.Size = new System.Drawing.Size(253, 17);
            this.lnkMichaelSwanson.TabIndex = 3;
            this.lnkMichaelSwanson.TabStop = true;
            this.lnkMichaelSwanson.Text = "SEAMonster was developed by Michael Swanson";
            this.lnkMichaelSwanson.UseCompatibleTextRendering = true;
            this.lnkMichaelSwanson.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkMichaelSwanson_LinkClicked);
            // 
            // About
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.CancelButton = this.btnOK;
            this.ClientSize = new System.Drawing.Size(525, 233);
            this.Controls.Add(this.lnkMichaelSwanson);
            this.Controls.Add(this.lnkSeamCarvingSite);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.pctSEAMonsterLogo);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "About";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About SEAMonster 0.2";
            ((System.ComponentModel.ISupportInitialize)(this.pctSEAMonsterLogo)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pctSEAMonsterLogo;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.LinkLabel lnkSeamCarvingSite;
        private System.Windows.Forms.LinkLabel lnkMichaelSwanson;
    }
}