namespace Microsoft.Test.Tools.WicCop
{
    partial class MessageForm
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
            this.messageGroupBox = new System.Windows.Forms.GroupBox( );
            this.messageTextBox = new System.Windows.Forms.TextBox( );
            this.splitContainer1 = new System.Windows.Forms.SplitContainer( );
            this.occuranceListBox = new System.Windows.Forms.ListBox( );
            this.noPropertiesTextBox = new System.Windows.Forms.TextBox( );
            this.messagePropertyGrid = new System.Windows.Forms.PropertyGrid( );
            this.closeButton = new System.Windows.Forms.Button( );
            this.nextButton = new System.Windows.Forms.Button( );
            this.previousButton = new System.Windows.Forms.Button( );
            this.panel1 = new System.Windows.Forms.Panel( );
            this.messageGroupBox.SuspendLayout( );
            this.splitContainer1.Panel1.SuspendLayout( );
            this.splitContainer1.Panel2.SuspendLayout( );
            this.splitContainer1.SuspendLayout( );
            this.panel1.SuspendLayout( );
            this.SuspendLayout( );
            // 
            // messageGroupBox
            // 
            this.messageGroupBox.Controls.Add( this.messageTextBox );
            this.messageGroupBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.messageGroupBox.Location = new System.Drawing.Point( 0, 0 );
            this.messageGroupBox.Name = "messageGroupBox";
            this.messageGroupBox.Size = new System.Drawing.Size( 733, 72 );
            this.messageGroupBox.TabIndex = 1;
            this.messageGroupBox.TabStop = false;
            this.messageGroupBox.Text = "Message";
            // 
            // messageTextBox
            // 
            this.messageTextBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.messageTextBox.Location = new System.Drawing.Point( 3, 16 );
            this.messageTextBox.Multiline = true;
            this.messageTextBox.Name = "messageTextBox";
            this.messageTextBox.ReadOnly = true;
            this.messageTextBox.Size = new System.Drawing.Size( 727, 40 );
            this.messageTextBox.TabIndex = 0;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point( 0, 72 );
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add( this.occuranceListBox );
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add( this.noPropertiesTextBox );
            this.splitContainer1.Panel2.Controls.Add( this.messagePropertyGrid );
            this.splitContainer1.Size = new System.Drawing.Size( 733, 274 );
            this.splitContainer1.SplitterDistance = 302;
            this.splitContainer1.TabIndex = 2;
            // 
            // occuranceListBox
            // 
            this.occuranceListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.occuranceListBox.FormattingEnabled = true;
            this.occuranceListBox.Location = new System.Drawing.Point( 0, 0 );
            this.occuranceListBox.Name = "occuranceListBox";
            this.occuranceListBox.Size = new System.Drawing.Size( 302, 264 );
            this.occuranceListBox.TabIndex = 0;
            this.occuranceListBox.SelectedIndexChanged += new System.EventHandler( this.occuranceListBox_SelectedIndexChanged );
            // 
            // noPropertiesTextBox
            // 
            this.noPropertiesTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.noPropertiesTextBox.Location = new System.Drawing.Point( 0, 0 );
            this.noPropertiesTextBox.Name = "noPropertiesTextBox";
            this.noPropertiesTextBox.ReadOnly = true;
            this.noPropertiesTextBox.Size = new System.Drawing.Size( 427, 20 );
            this.noPropertiesTextBox.TabIndex = 1;
            this.noPropertiesTextBox.Text = "No extended properties for the occurance";
            this.noPropertiesTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.noPropertiesTextBox.Visible = false;
            // 
            // messagePropertyGrid
            // 
            this.messagePropertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.messagePropertyGrid.Location = new System.Drawing.Point( 0, 0 );
            this.messagePropertyGrid.Name = "messagePropertyGrid";
            this.messagePropertyGrid.PropertySort = System.Windows.Forms.PropertySort.NoSort;
            this.messagePropertyGrid.Size = new System.Drawing.Size( 427, 274 );
            this.messagePropertyGrid.TabIndex = 0;
            // 
            // closeButton
            // 
            this.closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.closeButton.Location = new System.Drawing.Point( 649, 19 );
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size( 75, 23 );
            this.closeButton.TabIndex = 2;
            this.closeButton.Text = "Close";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler( this.closeButton_Click );
            // 
            // nextButton
            // 
            this.nextButton.Enabled = false;
            this.nextButton.Location = new System.Drawing.Point( 568, 19 );
            this.nextButton.Name = "nextButton";
            this.nextButton.Size = new System.Drawing.Size( 75, 23 );
            this.nextButton.TabIndex = 1;
            this.nextButton.Text = "Next";
            this.nextButton.UseVisualStyleBackColor = true;
            this.nextButton.Click += new System.EventHandler( this.nextButton_Click );
            // 
            // previousButton
            // 
            this.previousButton.Enabled = false;
            this.previousButton.Location = new System.Drawing.Point( 487, 19 );
            this.previousButton.Name = "previousButton";
            this.previousButton.Size = new System.Drawing.Size( 75, 23 );
            this.previousButton.TabIndex = 0;
            this.previousButton.Text = "Previous";
            this.previousButton.UseVisualStyleBackColor = true;
            this.previousButton.Click += new System.EventHandler( this.previousButton_Click );
            // 
            // panel1
            // 
            this.panel1.Controls.Add( this.closeButton );
            this.panel1.Controls.Add( this.nextButton );
            this.panel1.Controls.Add( this.previousButton );
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point( 0, 346 );
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size( 733, 53 );
            this.panel1.TabIndex = 0;
            // 
            // MessageForm
            // 
            this.AcceptButton = this.closeButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.closeButton;
            this.ClientSize = new System.Drawing.Size( 733, 399 );
            this.Controls.Add( this.splitContainer1 );
            this.Controls.Add( this.messageGroupBox );
            this.Controls.Add( this.panel1 );
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "MessageForm";
            this.ShowInTaskbar = false;
            this.Text = "Message Details";
            this.messageGroupBox.ResumeLayout( false );
            this.messageGroupBox.PerformLayout( );
            this.splitContainer1.Panel1.ResumeLayout( false );
            this.splitContainer1.Panel2.ResumeLayout( false );
            this.splitContainer1.Panel2.PerformLayout( );
            this.splitContainer1.ResumeLayout( false );
            this.panel1.ResumeLayout( false );
            this.ResumeLayout( false );

        }

        #endregion

        private System.Windows.Forms.GroupBox messageGroupBox;
        private System.Windows.Forms.TextBox messageTextBox;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ListBox occuranceListBox;
        private System.Windows.Forms.PropertyGrid messagePropertyGrid;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.Button nextButton;
        private System.Windows.Forms.Button previousButton;
        private System.Windows.Forms.TextBox noPropertiesTextBox;
        private System.Windows.Forms.Panel panel1;
    }
}