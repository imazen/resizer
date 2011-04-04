namespace SEAMonster
{
    partial class SEAMonsterClient
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.mnuFile = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuFileOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuFileSave = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuFileClose = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.mnuFileExit = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuEdit = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuEditCopyImage = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuView = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuViewEnergyMap = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuViewEnergyBias = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuAboutSEAMonster = new System.Windows.Forms.ToolStripMenuItem();
            this.pctImage = new System.Windows.Forms.PictureBox();
            this.btnCarve = new System.Windows.Forms.Button();
            this.btnSquash = new System.Windows.Forms.Button();
            this.pctEnergyMap = new System.Windows.Forms.PictureBox();
            this.lstEnergyFunctions = new System.Windows.Forms.ListBox();
            this.lstDirection = new System.Windows.Forms.ListBox();
            this.lstSeamMethod = new System.Windows.Forms.ListBox();
            this.txtX = new System.Windows.Forms.TextBox();
            this.lblX = new System.Windows.Forms.Label();
            this.lblY = new System.Windows.Forms.Label();
            this.txtY = new System.Windows.Forms.TextBox();
            this.lstBiasMode = new System.Windows.Forms.ListBox();
            this.lstSeamFunction = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.btnRevert = new System.Windows.Forms.Button();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pctImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pctEnergyMap)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuFile,
            this.mnuEdit,
            this.mnuView,
            this.mnuHelp});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(732, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // mnuFile
            // 
            this.mnuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuFileOpen,
            this.mnuFileSave,
            this.mnuFileClose,
            this.toolStripMenuItem1,
            this.mnuFileExit});
            this.mnuFile.Name = "mnuFile";
            this.mnuFile.Size = new System.Drawing.Size(37, 20);
            this.mnuFile.Text = "&File";
            // 
            // mnuFileOpen
            // 
            this.mnuFileOpen.Name = "mnuFileOpen";
            this.mnuFileOpen.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.mnuFileOpen.Size = new System.Drawing.Size(155, 22);
            this.mnuFileOpen.Text = "&Open...";
            this.mnuFileOpen.Click += new System.EventHandler(this.mnuFileOpen_Click);
            // 
            // mnuFileSave
            // 
            this.mnuFileSave.Name = "mnuFileSave";
            this.mnuFileSave.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.mnuFileSave.Size = new System.Drawing.Size(155, 22);
            this.mnuFileSave.Text = "&Save...";
            this.mnuFileSave.Click += new System.EventHandler(this.mnuFileSave_Click);
            // 
            // mnuFileClose
            // 
            this.mnuFileClose.Name = "mnuFileClose";
            this.mnuFileClose.Size = new System.Drawing.Size(155, 22);
            this.mnuFileClose.Text = "&Close";
            this.mnuFileClose.Click += new System.EventHandler(this.mnuFileClose_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(152, 6);
            // 
            // mnuFileExit
            // 
            this.mnuFileExit.Name = "mnuFileExit";
            this.mnuFileExit.Size = new System.Drawing.Size(155, 22);
            this.mnuFileExit.Text = "E&xit";
            this.mnuFileExit.Click += new System.EventHandler(this.mnuFileExit_Click);
            // 
            // mnuEdit
            // 
            this.mnuEdit.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuEditCopyImage});
            this.mnuEdit.Name = "mnuEdit";
            this.mnuEdit.Size = new System.Drawing.Size(39, 20);
            this.mnuEdit.Text = "&Edit";
            // 
            // mnuEditCopyImage
            // 
            this.mnuEditCopyImage.Name = "mnuEditCopyImage";
            this.mnuEditCopyImage.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.mnuEditCopyImage.Size = new System.Drawing.Size(180, 22);
            this.mnuEditCopyImage.Text = "&Copy Image";
            this.mnuEditCopyImage.Click += new System.EventHandler(this.mnuEditCopyImage_Click);
            // 
            // mnuView
            // 
            this.mnuView.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuViewEnergyMap,
            this.mnuViewEnergyBias});
            this.mnuView.Name = "mnuView";
            this.mnuView.Size = new System.Drawing.Size(44, 20);
            this.mnuView.Text = "&View";
            // 
            // mnuViewEnergyMap
            // 
            this.mnuViewEnergyMap.Name = "mnuViewEnergyMap";
            this.mnuViewEnergyMap.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
            this.mnuViewEnergyMap.Size = new System.Drawing.Size(177, 22);
            this.mnuViewEnergyMap.Text = "&Energy Map";
            this.mnuViewEnergyMap.Click += new System.EventHandler(this.mnuViewEnergyMap_Click);
            // 
            // mnuViewEnergyBias
            // 
            this.mnuViewEnergyBias.Name = "mnuViewEnergyBias";
            this.mnuViewEnergyBias.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.B)));
            this.mnuViewEnergyBias.Size = new System.Drawing.Size(177, 22);
            this.mnuViewEnergyBias.Text = "Energy &Bias";
            this.mnuViewEnergyBias.Click += new System.EventHandler(this.mnuViewEnergyBias_Click);
            // 
            // mnuHelp
            // 
            this.mnuHelp.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuAboutSEAMonster});
            this.mnuHelp.Name = "mnuHelp";
            this.mnuHelp.Size = new System.Drawing.Size(44, 20);
            this.mnuHelp.Text = "&Help";
            // 
            // mnuAboutSEAMonster
            // 
            this.mnuAboutSEAMonster.Name = "mnuAboutSEAMonster";
            this.mnuAboutSEAMonster.Size = new System.Drawing.Size(174, 22);
            this.mnuAboutSEAMonster.Text = "&About SEAMonster";
            this.mnuAboutSEAMonster.Click += new System.EventHandler(this.mnuAboutSEAMonster_Click);
            // 
            // pctImage
            // 
            this.pctImage.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.pctImage.Location = new System.Drawing.Point(12, 27);
            this.pctImage.Name = "pctImage";
            this.pctImage.Size = new System.Drawing.Size(610, 479);
            this.pctImage.TabIndex = 1;
            this.pctImage.TabStop = false;
            this.pctImage.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Bitmap_MouseMove);
            this.pctImage.Paint += new System.Windows.Forms.PaintEventHandler(this.Bitmap_Paint);
            // 
            // btnCarve
            // 
            this.btnCarve.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCarve.Location = new System.Drawing.Point(628, 56);
            this.btnCarve.Name = "btnCarve";
            this.btnCarve.Size = new System.Drawing.Size(92, 23);
            this.btnCarve.TabIndex = 0;
            this.btnCarve.Text = "&Carve";
            this.btnCarve.UseVisualStyleBackColor = true;
            this.btnCarve.Click += new System.EventHandler(this.btnCarve_Click);
            // 
            // btnSquash
            // 
            this.btnSquash.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSquash.Location = new System.Drawing.Point(628, 85);
            this.btnSquash.Name = "btnSquash";
            this.btnSquash.Size = new System.Drawing.Size(92, 23);
            this.btnSquash.TabIndex = 1;
            this.btnSquash.Text = "&Squash";
            this.btnSquash.UseVisualStyleBackColor = true;
            this.btnSquash.Click += new System.EventHandler(this.btnSquash_Click);
            // 
            // pctEnergyMap
            // 
            this.pctEnergyMap.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.pctEnergyMap.Location = new System.Drawing.Point(12, 27);
            this.pctEnergyMap.Name = "pctEnergyMap";
            this.pctEnergyMap.Size = new System.Drawing.Size(610, 479);
            this.pctEnergyMap.TabIndex = 4;
            this.pctEnergyMap.TabStop = false;
            this.pctEnergyMap.Visible = false;
            this.pctEnergyMap.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Bitmap_MouseMove);
            this.pctEnergyMap.Paint += new System.Windows.Forms.PaintEventHandler(this.Bitmap_Paint);
            // 
            // lstEnergyFunctions
            // 
            this.lstEnergyFunctions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lstEnergyFunctions.FormattingEnabled = true;
            this.lstEnergyFunctions.Location = new System.Drawing.Point(628, 424);
            this.lstEnergyFunctions.Name = "lstEnergyFunctions";
            this.lstEnergyFunctions.Size = new System.Drawing.Size(92, 82);
            this.lstEnergyFunctions.TabIndex = 8;
            this.lstEnergyFunctions.SelectedIndexChanged += new System.EventHandler(this.lstEnergyFunctions_SelectedIndexChanged);
            // 
            // lstDirection
            // 
            this.lstDirection.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lstDirection.FormattingEnabled = true;
            this.lstDirection.Items.AddRange(new object[] {
            "Vertical",
            "Horizontal",
            "Optimal"});
            this.lstDirection.Location = new System.Drawing.Point(628, 181);
            this.lstDirection.Name = "lstDirection";
            this.lstDirection.Size = new System.Drawing.Size(92, 43);
            this.lstDirection.TabIndex = 6;
            // 
            // lstSeamMethod
            // 
            this.lstSeamMethod.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lstSeamMethod.FormattingEnabled = true;
            this.lstSeamMethod.Items.AddRange(new object[] {
            "Total",
            "Average",
            "DiffBias"});
            this.lstSeamMethod.Location = new System.Drawing.Point(628, 296);
            this.lstSeamMethod.Name = "lstSeamMethod";
            this.lstSeamMethod.Size = new System.Drawing.Size(92, 43);
            this.lstSeamMethod.TabIndex = 7;
            // 
            // txtX
            // 
            this.txtX.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtX.Location = new System.Drawing.Point(645, 114);
            this.txtX.Name = "txtX";
            this.txtX.Size = new System.Drawing.Size(75, 20);
            this.txtX.TabIndex = 3;
            // 
            // lblX
            // 
            this.lblX.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblX.AutoSize = true;
            this.lblX.Location = new System.Drawing.Point(628, 117);
            this.lblX.Name = "lblX";
            this.lblX.Size = new System.Drawing.Size(17, 13);
            this.lblX.TabIndex = 2;
            this.lblX.Text = "X:";
            // 
            // lblY
            // 
            this.lblY.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblY.AutoSize = true;
            this.lblY.Location = new System.Drawing.Point(628, 143);
            this.lblY.Name = "lblY";
            this.lblY.Size = new System.Drawing.Size(17, 13);
            this.lblY.TabIndex = 4;
            this.lblY.Text = "Y:";
            // 
            // txtY
            // 
            this.txtY.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtY.Location = new System.Drawing.Point(645, 140);
            this.txtY.Name = "txtY";
            this.txtY.Size = new System.Drawing.Size(75, 20);
            this.txtY.TabIndex = 5;
            // 
            // lstBiasMode
            // 
            this.lstBiasMode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lstBiasMode.FormattingEnabled = true;
            this.lstBiasMode.Items.AddRange(new object[] {
            "Add",
            "Subtract",
            "Clear"});
            this.lstBiasMode.Location = new System.Drawing.Point(628, 360);
            this.lstBiasMode.Name = "lstBiasMode";
            this.lstBiasMode.Size = new System.Drawing.Size(92, 43);
            this.lstBiasMode.TabIndex = 10;
            // 
            // lstSeamFunction
            // 
            this.lstSeamFunction.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lstSeamFunction.FormattingEnabled = true;
            this.lstSeamFunction.Location = new System.Drawing.Point(628, 245);
            this.lstSeamFunction.Name = "lstSeamFunction";
            this.lstSeamFunction.Size = new System.Drawing.Size(92, 30);
            this.lstSeamFunction.TabIndex = 11;
            this.lstSeamFunction.SelectedIndexChanged += new System.EventHandler(this.lstSeamFunction_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.label1.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.label1.Location = new System.Drawing.Point(628, 408);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(92, 15);
            this.label1.TabIndex = 12;
            this.label1.Text = "Energy Function";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.label2.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.label2.Location = new System.Drawing.Point(628, 229);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(92, 15);
            this.label2.TabIndex = 13;
            this.label2.Text = "Seam Function";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.label3.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.label3.Location = new System.Drawing.Point(628, 344);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(92, 15);
            this.label3.TabIndex = 14;
            this.label3.Text = "Bias Mode";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.label4.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.label4.Location = new System.Drawing.Point(628, 280);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(92, 15);
            this.label4.TabIndex = 15;
            this.label4.Text = "Seam Compare";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.label5.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.label5.Location = new System.Drawing.Point(628, 165);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(92, 15);
            this.label5.TabIndex = 16;
            this.label5.Text = "Direction";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnRevert
            // 
            this.btnRevert.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRevert.Location = new System.Drawing.Point(628, 27);
            this.btnRevert.Name = "btnRevert";
            this.btnRevert.Size = new System.Drawing.Size(92, 23);
            this.btnRevert.TabIndex = 17;
            this.btnRevert.Text = "&Revert";
            this.btnRevert.UseVisualStyleBackColor = true;
            this.btnRevert.Click += new System.EventHandler(this.btnRevert_Click);
            // 
            // SEAMonsterClient
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(732, 518);
            this.Controls.Add(this.btnRevert);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lstSeamFunction);
            this.Controls.Add(this.lstBiasMode);
            this.Controls.Add(this.lblY);
            this.Controls.Add(this.txtY);
            this.Controls.Add(this.lblX);
            this.Controls.Add(this.txtX);
            this.Controls.Add(this.lstSeamMethod);
            this.Controls.Add(this.lstDirection);
            this.Controls.Add(this.lstEnergyFunctions);
            this.Controls.Add(this.pctEnergyMap);
            this.Controls.Add(this.btnSquash);
            this.Controls.Add(this.btnCarve);
            this.Controls.Add(this.pctImage);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "SEAMonsterClient";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SEAMonster 0.2";
            this.Load += new System.EventHandler(this.SEAMonsterClient_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pctImage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pctEnergyMap)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem mnuFile;
        private System.Windows.Forms.ToolStripMenuItem mnuHelp;
        private System.Windows.Forms.ToolStripMenuItem mnuFileExit;
        private System.Windows.Forms.ToolStripMenuItem mnuAboutSEAMonster;
        private System.Windows.Forms.ToolStripMenuItem mnuFileOpen;
        private System.Windows.Forms.ToolStripMenuItem mnuFileClose;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem mnuView;
        private System.Windows.Forms.ToolStripMenuItem mnuViewEnergyMap;
        private System.Windows.Forms.ToolStripMenuItem mnuViewEnergyBias;
        private System.Windows.Forms.ToolStripMenuItem mnuEdit;
        private System.Windows.Forms.ToolStripMenuItem mnuEditCopyImage;
        private System.Windows.Forms.PictureBox pctImage;
        private System.Windows.Forms.Button btnCarve;
        private System.Windows.Forms.Button btnSquash;
        private System.Windows.Forms.PictureBox pctEnergyMap;
        private System.Windows.Forms.ListBox lstEnergyFunctions;
        private System.Windows.Forms.ListBox lstDirection;
        private System.Windows.Forms.ListBox lstSeamMethod;
        private System.Windows.Forms.TextBox txtX;
        private System.Windows.Forms.Label lblX;
        private System.Windows.Forms.Label lblY;
        private System.Windows.Forms.TextBox txtY;
        private System.Windows.Forms.ListBox lstBiasMode;
        private System.Windows.Forms.ListBox lstSeamFunction;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ToolStripMenuItem mnuFileSave;
        private System.Windows.Forms.Button btnRevert;
    }
}

