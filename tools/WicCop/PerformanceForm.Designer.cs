namespace Microsoft.Test.Tools.WicCop
{
    partial class PerformanceForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose( bool disposing )
        {
            if ( disposing && ( components != null ) )
            {
                components.Dispose( );
            }
            base.Dispose( disposing );
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent( )
        {
            this.okButton = new System.Windows.Forms.Button();
            this.rawPictureBox = new System.Windows.Forms.PictureBox();
            this.rawGroupBox = new System.Windows.Forms.GroupBox();
            this.renderModeComboBox = new System.Windows.Forms.ComboBox();
            this.renderModeLabel = new System.Windows.Forms.Label();
            this.noiseReductionGroupBox = new System.Windows.Forms.GroupBox();
            this.noiseReductionTrackBar = new System.Windows.Forms.TrackBar();
            this.sharpnessGroupBox = new System.Windows.Forms.GroupBox();
            this.sharpnessTrackBar = new System.Windows.Forms.TrackBar();
            this.saturationGroupBox = new System.Windows.Forms.GroupBox();
            this.saturationTrackBar = new System.Windows.Forms.TrackBar();
            this.contrastGroupBox = new System.Windows.Forms.GroupBox();
            this.contrastTrackBar = new System.Windows.Forms.TrackBar();
            this.gammaGroupBox = new System.Windows.Forms.GroupBox();
            this.gammaTrackBar = new System.Windows.Forms.TrackBar();
            this.exposureGroupBox = new System.Windows.Forms.GroupBox();
            this.exposureTrackBar = new System.Windows.Forms.TrackBar();
            this.tintGroupBox = new System.Windows.Forms.GroupBox();
            this.tintTrackBar = new System.Windows.Forms.TrackBar();
            this.temperatureGroupBox = new System.Windows.Forms.GroupBox();
            this.temperatureTrackBar = new System.Windows.Forms.TrackBar();
            this.filesListView = new System.Windows.Forms.ListView();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.backgroundWorker = new System.ComponentModel.BackgroundWorker();
            ((System.ComponentModel.ISupportInitialize)(this.rawPictureBox)).BeginInit();
            this.rawGroupBox.SuspendLayout();
            this.noiseReductionGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.noiseReductionTrackBar)).BeginInit();
            this.sharpnessGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sharpnessTrackBar)).BeginInit();
            this.saturationGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.saturationTrackBar)).BeginInit();
            this.contrastGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.contrastTrackBar)).BeginInit();
            this.gammaGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gammaTrackBar)).BeginInit();
            this.exposureGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.exposureTrackBar)).BeginInit();
            this.tintGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tintTrackBar)).BeginInit();
            this.temperatureGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.temperatureTrackBar)).BeginInit();
            this.SuspendLayout();
            // 
            // okButton
            // 
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.okButton.Location = new System.Drawing.Point(931, 837);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 1;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // rawPictureBox
            // 
            this.rawPictureBox.Location = new System.Drawing.Point(366, 3);
            this.rawPictureBox.Name = "rawPictureBox";
            this.rawPictureBox.Size = new System.Drawing.Size(640, 480);
            this.rawPictureBox.TabIndex = 2;
            this.rawPictureBox.TabStop = false;
            // 
            // rawGroupBox
            // 
            this.rawGroupBox.Controls.Add(this.renderModeComboBox);
            this.rawGroupBox.Controls.Add(this.renderModeLabel);
            this.rawGroupBox.Controls.Add(this.noiseReductionGroupBox);
            this.rawGroupBox.Controls.Add(this.sharpnessGroupBox);
            this.rawGroupBox.Controls.Add(this.saturationGroupBox);
            this.rawGroupBox.Controls.Add(this.contrastGroupBox);
            this.rawGroupBox.Controls.Add(this.gammaGroupBox);
            this.rawGroupBox.Controls.Add(this.exposureGroupBox);
            this.rawGroupBox.Controls.Add(this.tintGroupBox);
            this.rawGroupBox.Controls.Add(this.temperatureGroupBox);
            this.rawGroupBox.Location = new System.Drawing.Point(12, 489);
            this.rawGroupBox.Name = "rawGroupBox";
            this.rawGroupBox.Size = new System.Drawing.Size(994, 342);
            this.rawGroupBox.TabIndex = 3;
            this.rawGroupBox.TabStop = false;
            this.rawGroupBox.Text = "RAW Adjustments";
            // 
            // renderModeComboBox
            // 
            this.renderModeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.renderModeComboBox.Enabled = false;
            this.renderModeComboBox.FormattingEnabled = true;
            this.renderModeComboBox.Items.AddRange(new object[] {
            "Unknown",
            "Draft",
            "Normal",
            "Best Quality"});
            this.renderModeComboBox.Location = new System.Drawing.Point(95, 301);
            this.renderModeComboBox.Name = "renderModeComboBox";
            this.renderModeComboBox.Size = new System.Drawing.Size(121, 21);
            this.renderModeComboBox.TabIndex = 6;
            this.renderModeComboBox.SelectedValueChanged += new System.EventHandler(this.renderModeComboBox_SelectedValueChanged);
            // 
            // renderModeLabel
            // 
            this.renderModeLabel.AutoSize = true;
            this.renderModeLabel.Location = new System.Drawing.Point(13, 305);
            this.renderModeLabel.Name = "renderModeLabel";
            this.renderModeLabel.Size = new System.Drawing.Size(75, 13);
            this.renderModeLabel.TabIndex = 5;
            this.renderModeLabel.Text = "Render Mode:";
            // 
            // noiseReductionGroupBox
            // 
            this.noiseReductionGroupBox.Controls.Add(this.noiseReductionTrackBar);
            this.noiseReductionGroupBox.Location = new System.Drawing.Point(518, 230);
            this.noiseReductionGroupBox.Name = "noiseReductionGroupBox";
            this.noiseReductionGroupBox.Size = new System.Drawing.Size(470, 64);
            this.noiseReductionGroupBox.TabIndex = 4;
            this.noiseReductionGroupBox.TabStop = false;
            this.noiseReductionGroupBox.Text = "Noise Reduction";
            // 
            // noiseReductionTrackBar
            // 
            this.noiseReductionTrackBar.Enabled = false;
            this.noiseReductionTrackBar.Location = new System.Drawing.Point(6, 12);
            this.noiseReductionTrackBar.Name = "noiseReductionTrackBar";
            this.noiseReductionTrackBar.Size = new System.Drawing.Size(458, 45);
            this.noiseReductionTrackBar.TabIndex = 1;
            this.noiseReductionTrackBar.TickStyle = System.Windows.Forms.TickStyle.Both;
            this.noiseReductionTrackBar.ValueChanged += new System.EventHandler(this.noiseReductionTrackBar_ValueChanged);
            // 
            // sharpnessGroupBox
            // 
            this.sharpnessGroupBox.Controls.Add(this.sharpnessTrackBar);
            this.sharpnessGroupBox.Location = new System.Drawing.Point(7, 230);
            this.sharpnessGroupBox.Name = "sharpnessGroupBox";
            this.sharpnessGroupBox.Size = new System.Drawing.Size(470, 64);
            this.sharpnessGroupBox.TabIndex = 3;
            this.sharpnessGroupBox.TabStop = false;
            this.sharpnessGroupBox.Text = "Sharpness";
            // 
            // sharpnessTrackBar
            // 
            this.sharpnessTrackBar.Enabled = false;
            this.sharpnessTrackBar.Location = new System.Drawing.Point(6, 12);
            this.sharpnessTrackBar.Name = "sharpnessTrackBar";
            this.sharpnessTrackBar.Size = new System.Drawing.Size(458, 45);
            this.sharpnessTrackBar.TabIndex = 1;
            this.sharpnessTrackBar.TickStyle = System.Windows.Forms.TickStyle.Both;
            this.sharpnessTrackBar.ValueChanged += new System.EventHandler(this.sharpnessTrackBar_ValueChanged);
            // 
            // saturationGroupBox
            // 
            this.saturationGroupBox.Controls.Add(this.saturationTrackBar);
            this.saturationGroupBox.Location = new System.Drawing.Point(518, 160);
            this.saturationGroupBox.Name = "saturationGroupBox";
            this.saturationGroupBox.Size = new System.Drawing.Size(470, 64);
            this.saturationGroupBox.TabIndex = 2;
            this.saturationGroupBox.TabStop = false;
            this.saturationGroupBox.Text = "Saturation";
            // 
            // saturationTrackBar
            // 
            this.saturationTrackBar.Enabled = false;
            this.saturationTrackBar.Location = new System.Drawing.Point(6, 12);
            this.saturationTrackBar.Minimum = -10;
            this.saturationTrackBar.Name = "saturationTrackBar";
            this.saturationTrackBar.Size = new System.Drawing.Size(458, 45);
            this.saturationTrackBar.TabIndex = 1;
            this.saturationTrackBar.TickStyle = System.Windows.Forms.TickStyle.Both;
            this.saturationTrackBar.ValueChanged += new System.EventHandler(this.saturationTrackBar_ValueChanged);
            // 
            // contrastGroupBox
            // 
            this.contrastGroupBox.Controls.Add(this.contrastTrackBar);
            this.contrastGroupBox.Location = new System.Drawing.Point(6, 160);
            this.contrastGroupBox.Name = "contrastGroupBox";
            this.contrastGroupBox.Size = new System.Drawing.Size(470, 64);
            this.contrastGroupBox.TabIndex = 2;
            this.contrastGroupBox.TabStop = false;
            this.contrastGroupBox.Text = "Contrast";
            // 
            // contrastTrackBar
            // 
            this.contrastTrackBar.Enabled = false;
            this.contrastTrackBar.Location = new System.Drawing.Point(6, 12);
            this.contrastTrackBar.Name = "contrastTrackBar";
            this.contrastTrackBar.Size = new System.Drawing.Size(458, 45);
            this.contrastTrackBar.TabIndex = 1;
            this.contrastTrackBar.TickStyle = System.Windows.Forms.TickStyle.Both;
            this.contrastTrackBar.ValueChanged += new System.EventHandler(this.contrastTrackBar_ValueChanged);
            // 
            // gammaGroupBox
            // 
            this.gammaGroupBox.Controls.Add(this.gammaTrackBar);
            this.gammaGroupBox.Location = new System.Drawing.Point(518, 90);
            this.gammaGroupBox.Name = "gammaGroupBox";
            this.gammaGroupBox.Size = new System.Drawing.Size(470, 64);
            this.gammaGroupBox.TabIndex = 2;
            this.gammaGroupBox.TabStop = false;
            this.gammaGroupBox.Text = "Gamma";
            // 
            // gammaTrackBar
            // 
            this.gammaTrackBar.Enabled = false;
            this.gammaTrackBar.Location = new System.Drawing.Point(6, 12);
            this.gammaTrackBar.Maximum = 5;
            this.gammaTrackBar.Name = "gammaTrackBar";
            this.gammaTrackBar.Size = new System.Drawing.Size(458, 45);
            this.gammaTrackBar.TabIndex = 1;
            this.gammaTrackBar.TickStyle = System.Windows.Forms.TickStyle.Both;
            this.gammaTrackBar.Value = 1;
            this.gammaTrackBar.ValueChanged += new System.EventHandler(this.gammaTrackBar_ValueChanged);
            // 
            // exposureGroupBox
            // 
            this.exposureGroupBox.Controls.Add(this.exposureTrackBar);
            this.exposureGroupBox.Location = new System.Drawing.Point(7, 90);
            this.exposureGroupBox.Name = "exposureGroupBox";
            this.exposureGroupBox.Size = new System.Drawing.Size(470, 64);
            this.exposureGroupBox.TabIndex = 2;
            this.exposureGroupBox.TabStop = false;
            this.exposureGroupBox.Text = "Exposure (eV)";
            // 
            // exposureTrackBar
            // 
            this.exposureTrackBar.Enabled = false;
            this.exposureTrackBar.Location = new System.Drawing.Point(6, 12);
            this.exposureTrackBar.Maximum = 5;
            this.exposureTrackBar.Minimum = -5;
            this.exposureTrackBar.Name = "exposureTrackBar";
            this.exposureTrackBar.Size = new System.Drawing.Size(458, 45);
            this.exposureTrackBar.TabIndex = 1;
            this.exposureTrackBar.TickStyle = System.Windows.Forms.TickStyle.Both;
            this.exposureTrackBar.ValueChanged += new System.EventHandler(this.exposureTrackBar_ValueChanged);
            // 
            // tintGroupBox
            // 
            this.tintGroupBox.Controls.Add(this.tintTrackBar);
            this.tintGroupBox.Location = new System.Drawing.Point(518, 20);
            this.tintGroupBox.Name = "tintGroupBox";
            this.tintGroupBox.Size = new System.Drawing.Size(470, 64);
            this.tintGroupBox.TabIndex = 2;
            this.tintGroupBox.TabStop = false;
            this.tintGroupBox.Text = "Tint";
            // 
            // tintTrackBar
            // 
            this.tintTrackBar.Enabled = false;
            this.tintTrackBar.Location = new System.Drawing.Point(6, 12);
            this.tintTrackBar.Minimum = -10;
            this.tintTrackBar.Name = "tintTrackBar";
            this.tintTrackBar.Size = new System.Drawing.Size(458, 45);
            this.tintTrackBar.TabIndex = 1;
            this.tintTrackBar.TickStyle = System.Windows.Forms.TickStyle.Both;
            this.tintTrackBar.ValueChanged += new System.EventHandler(this.tintTrackBar_ValueChanged);
            // 
            // temperatureGroupBox
            // 
            this.temperatureGroupBox.Controls.Add(this.temperatureTrackBar);
            this.temperatureGroupBox.Location = new System.Drawing.Point(7, 20);
            this.temperatureGroupBox.Name = "temperatureGroupBox";
            this.temperatureGroupBox.Size = new System.Drawing.Size(470, 64);
            this.temperatureGroupBox.TabIndex = 0;
            this.temperatureGroupBox.TabStop = false;
            this.temperatureGroupBox.Text = "Temperature (K)";
            // 
            // temperatureTrackBar
            // 
            this.temperatureTrackBar.Enabled = false;
            this.temperatureTrackBar.Location = new System.Drawing.Point(6, 12);
            this.temperatureTrackBar.Maximum = 30000;
            this.temperatureTrackBar.Minimum = 1500;
            this.temperatureTrackBar.Name = "temperatureTrackBar";
            this.temperatureTrackBar.Size = new System.Drawing.Size(458, 45);
            this.temperatureTrackBar.TabIndex = 1;
            this.temperatureTrackBar.TickFrequency = 500;
            this.temperatureTrackBar.TickStyle = System.Windows.Forms.TickStyle.Both;
            this.temperatureTrackBar.Value = 1500;
            this.temperatureTrackBar.ValueChanged += new System.EventHandler(this.temperatureTrackBar_ValueChanged);
            // 
            // filesListView
            // 
            this.filesListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.filesListView.Location = new System.Drawing.Point(12, 3);
            this.filesListView.MultiSelect = false;
            this.filesListView.Name = "filesListView";
            this.filesListView.Size = new System.Drawing.Size(348, 480);
            this.filesListView.TabIndex = 4;
            this.filesListView.UseCompatibleStateImageBehavior = false;
            this.filesListView.View = System.Windows.Forms.View.Details;
            this.filesListView.SelectedIndexChanged += new System.EventHandler(this.filesListView_SelectedIndexChanged);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "File Path";
            // 
            // backgroundWorker
            // 
            this.backgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker_DoWork);
            // 
            // PerformanceForm
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.okButton;
            this.ClientSize = new System.Drawing.Size(1018, 872);
            this.Controls.Add(this.filesListView);
            this.Controls.Add(this.rawGroupBox);
            this.Controls.Add(this.rawPictureBox);
            this.Controls.Add(this.okButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "PerformanceForm";
            this.ShowInTaskbar = false;
            this.Text = "Real Time Performance Test";
            ((System.ComponentModel.ISupportInitialize)(this.rawPictureBox)).EndInit();
            this.rawGroupBox.ResumeLayout(false);
            this.rawGroupBox.PerformLayout();
            this.noiseReductionGroupBox.ResumeLayout(false);
            this.noiseReductionGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.noiseReductionTrackBar)).EndInit();
            this.sharpnessGroupBox.ResumeLayout(false);
            this.sharpnessGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sharpnessTrackBar)).EndInit();
            this.saturationGroupBox.ResumeLayout(false);
            this.saturationGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.saturationTrackBar)).EndInit();
            this.contrastGroupBox.ResumeLayout(false);
            this.contrastGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.contrastTrackBar)).EndInit();
            this.gammaGroupBox.ResumeLayout(false);
            this.gammaGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gammaTrackBar)).EndInit();
            this.exposureGroupBox.ResumeLayout(false);
            this.exposureGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.exposureTrackBar)).EndInit();
            this.tintGroupBox.ResumeLayout(false);
            this.tintGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tintTrackBar)).EndInit();
            this.temperatureGroupBox.ResumeLayout(false);
            this.temperatureGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.temperatureTrackBar)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.PictureBox rawPictureBox;
        private System.Windows.Forms.GroupBox rawGroupBox;
        private System.Windows.Forms.GroupBox temperatureGroupBox;
        private System.Windows.Forms.TrackBar temperatureTrackBar;
        private System.Windows.Forms.GroupBox saturationGroupBox;
        private System.Windows.Forms.TrackBar saturationTrackBar;
        private System.Windows.Forms.GroupBox contrastGroupBox;
        private System.Windows.Forms.TrackBar contrastTrackBar;
        private System.Windows.Forms.GroupBox gammaGroupBox;
        private System.Windows.Forms.TrackBar gammaTrackBar;
        private System.Windows.Forms.GroupBox exposureGroupBox;
        private System.Windows.Forms.TrackBar exposureTrackBar;
        private System.Windows.Forms.GroupBox tintGroupBox;
        private System.Windows.Forms.TrackBar tintTrackBar;
        private System.Windows.Forms.GroupBox noiseReductionGroupBox;
        private System.Windows.Forms.TrackBar noiseReductionTrackBar;
        private System.Windows.Forms.GroupBox sharpnessGroupBox;
        private System.Windows.Forms.TrackBar sharpnessTrackBar;
        private System.Windows.Forms.Label renderModeLabel;
        private System.Windows.Forms.ComboBox renderModeComboBox;
        private System.Windows.Forms.ListView filesListView;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.ComponentModel.BackgroundWorker backgroundWorker;
    }
}