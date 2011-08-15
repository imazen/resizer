using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

using System.Drawing.Imaging;

using SEAMonster.EnergyFunctions;
using SEAMonster.SeamFunctions;

namespace SEAMonster
{
    public partial class SEAMonsterClient : Form
    {
        private SMImage smImage = null;             // The image to carve
        private string fileName = string.Empty;     // The file we opened
        private int brushRadius = 10;               // Radius of the energy bias brush
        private Bitmap energyBiasBitmap = null;     // Bitmap for energy bias

        public SEAMonsterClient()
        {
            InitializeComponent();
        }

        private void mnuFileExit_Click(object sender, EventArgs e)
        {
            // Exit the application
            this.Close();
        }

        private void mnuFileOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "JPEG (*.jpg;*.jpeg;*.jpe)|*.jpg;*.jpeg;*.jpe|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                OpenFile(openFileDialog.FileName);
            }
        }

        private void OpenFile(string fileName)
        {
            this.fileName = fileName;

            Bitmap bitmap = new Bitmap(this.fileName);
            pctImage.Image = bitmap;

            // Create a seam carving image
            this.smImage = new SMImage(bitmap, (SeamFunction)lstSeamFunction.SelectedItem, (EnergyFunction)lstEnergyFunctions.SelectedItem);
            this.smImage.ImageChanged += new EventHandler(smImage_ImageChanged);

            // Update the dimensions
            UpdateDimensions();

            ToggleControls(true);

            UpdateTitle();
        }

        private void UpdateTitle()
        {
            this.Text = "SEAMonster 0.2";

            if (this.fileName != string.Empty)
            {
                this.Text += " - " + this.fileName.Substring(this.fileName.LastIndexOf(@"\") + 1);
            }
        }

        private void UpdateDimensions()
        {
            // Set X and Y dimensions
            txtX.Text = this.smImage.Size.Width.ToString();
            txtY.Text = this.smImage.Size.Height.ToString();
        }

        void smImage_ImageChanged(object sender, EventArgs e)
        {
            // Image changed, so refresh
            if (mnuViewEnergyBias.Checked)
            {
                // TODO: Expensive to re-generate the bitmap each refresh
                energyBiasBitmap = this.smImage.EnergyBiasBitmap;
            }

            if (mnuViewEnergyMap.Checked)
            {
                // TODO: Expensive to re-generate the bitmap each refresh
                pctEnergyMap.Image = this.smImage.EnergyMapBitmap;
                pctEnergyMap.Refresh();
            }
            else
            {
                pctImage.Refresh();
            }

            // Update the dimensions
            UpdateDimensions();

            // Be a good citizen
            Application.DoEvents();
        }

        private void btnCarve_Click(object sender, EventArgs e)
        {
            // If the energy bias is being displayed, we need to parse it
            // TODO: This is a hack. Should be read directly from the bitmap without any parsing.
            if (mnuViewEnergyBias.Checked)
            {
                this.smImage.EnergyBiasBitmap = energyBiasBitmap;
            }

            // Which direction are we carving?
            Direction direction = (Direction)Enum.Parse(typeof(Direction), lstDirection.SelectedItem.ToString());

            ComparisonMethod comparisonMethod = (ComparisonMethod)Enum.Parse(typeof(ComparisonMethod), lstSeamMethod.SelectedItem.ToString());
            
            // Carve a single slice from the image
            Size minimumSize = new Size(this.smImage.Size.Width - 1, this.smImage.Size.Height - 1);
            this.smImage.Carve(direction, comparisonMethod, minimumSize);
        }

        private void btnSquash_Click(object sender, EventArgs e)
        {
            ToggleControls(false);

            // If the energy bias is being displayed, we need to parse it
            // TODO: This is a hack. Should be read directly from the bitmap without any parsing.
            if (mnuViewEnergyBias.Checked)
            {
                this.smImage.EnergyBiasBitmap = energyBiasBitmap;
            }

            // Which direction are we squashing?
            Direction direction = (Direction)Enum.Parse(typeof(Direction), lstDirection.SelectedItem.ToString());

            ComparisonMethod comparisonMethod = (ComparisonMethod)Enum.Parse(typeof(ComparisonMethod), lstSeamMethod.SelectedItem.ToString());

            // Squash the whole image
            Size minimumSize = new Size(Convert.ToInt16(txtX.Text), Convert.ToInt16(txtY.Text));
            this.smImage.Carve(direction, comparisonMethod, minimumSize);

            ToggleControls(true);
        }

        private void ToggleControls(bool enabled)
       { 
            foreach (Control control in this.Controls)
            {
                if (!(control is MenuStrip))
                {
                    control.Enabled = enabled;
                }
            }
        }

        private void SEAMonsterClient_Load(object sender, EventArgs e)
        {
            ToggleControls(false);

            // Add available energy functions
            // TODO: This should be in config file
            lstEnergyFunctions.Items.Add(new EnergyFunctions.Sobel());
            lstEnergyFunctions.Items.Add(new EnergyFunctions.Luminance());
            lstEnergyFunctions.Items.Add(new EnergyFunctions.Red());
            lstEnergyFunctions.Items.Add(new EnergyFunctions.Green());
            lstEnergyFunctions.Items.Add(new EnergyFunctions.Blue());
            lstEnergyFunctions.Items.Add(new EnergyFunctions.Random());
            lstEnergyFunctions.SelectedIndex = 0;

            // Add seam functions
            // TODO: This should be in config file
            lstSeamFunction.Items.Add(new CumulativeEnergy());
            lstSeamFunction.Items.Add(new Standard());
            lstSeamFunction.SelectedIndex = 0;

            // Select "vertical" as default direction
            lstDirection.SelectedIndex = 0;

            // Select default seam comparison method
            lstSeamMethod.SelectedIndex = 0;

            // Select first bias mode (Add)
            lstBiasMode.SelectedIndex = 0;
        }

        private void lstEnergyFunctions_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.smImage != null && lstEnergyFunctions.SelectedItem != null)
            {
                // Switch energy functions
                this.smImage.EnergyFunction = (EnergyFunction)lstEnergyFunctions.SelectedItem;

                // Refresh map
                pctEnergyMap.Image = this.smImage.EnergyMapBitmap;
            }
        }

        private void mnuViewEnergyMap_Click(object sender, EventArgs e)
        {
            ToggleEnergyMap();
        }

        private void mnuViewEnergyBias_Click(object sender, EventArgs e)
        {
            ToggleEnergyBias();
        }

        private void ToggleEnergyBias()
        {
            if (this.smImage != null)
            {
                // Toggle the setting
                mnuViewEnergyBias.Checked = !mnuViewEnergyBias.Checked;

                // If enabling, make sure we have the latest energy bias map
                if (mnuViewEnergyBias.Checked)
                {
                    energyBiasBitmap = this.smImage.EnergyBiasBitmap;

                    // Change cursors
                    pctImage.Cursor = Cursors.Cross;
                    pctEnergyMap.Cursor = Cursors.Cross;
                }
                else
                {
                    // Set energy bias map
                    this.smImage.EnergyBiasBitmap = energyBiasBitmap;

                    // Change cursors
                    pctImage.Cursor = Cursors.Default;
                    pctEnergyMap.Cursor = Cursors.Default;
                }

                pctImage.Refresh();
                pctEnergyMap.Refresh();
            }
        }

        private void ToggleEnergyMap()
        {
            if (this.smImage != null)
            {
                // Toggle the setting
                mnuViewEnergyMap.Checked = !mnuViewEnergyMap.Checked;

                // If enabling, make sure we have the latest energy map
                if (mnuViewEnergyMap.Checked)
                {
                    pctEnergyMap.Image = this.smImage.EnergyMapBitmap;
                }

                // Show/hide energy map
                pctEnergyMap.Visible = mnuViewEnergyMap.Checked;
            }
        }

        private void lstSeamFunction_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.smImage != null && lstSeamFunction.SelectedItem != null)
            {
                // Switch seam function
                this.smImage.SeamFunction = (SeamFunction)lstSeamFunction.SelectedItem;
            }
        }

        private void mnuFileSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.Filter = "JPEG (*.jpg;*.jpeg;*.jpe)|*.jpg;*.jpeg;*.jpe|All files (*.*)|*.*";
            saveFileDialog.FilterIndex = 1;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                this.fileName = saveFileDialog.FileName;

                ImageCodecInfo myImageCodecInfo;
                Encoder myEncoder;
                EncoderParameter myEncoderParameter;
                EncoderParameters myEncoderParameters;

                // Get an ImageCodecInfo object that represents the JPEG codec.
                myImageCodecInfo = GetEncoderInfo("image/jpeg");
                myEncoder = Encoder.Quality;
                myEncoderParameters = new EncoderParameters(1);
                myEncoderParameter = new EncoderParameter(myEncoder, 75L);          // Must be a long
                myEncoderParameters.Param[0] = myEncoderParameter;
                this.smImage.CarvedBitmap.Save(this.fileName, myImageCodecInfo, myEncoderParameters);

                // Open the file we just saved
                OpenFile(this.fileName);
            }
        }

        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }

        private void mnuFileClose_Click(object sender, EventArgs e)
        {
            ToggleControls(false);
            if (mnuViewEnergyMap.Checked)
            {
                ToggleEnergyMap();
            }
            if (mnuViewEnergyBias.Checked)
            {
                ToggleEnergyBias();
            }
            this.smImage = null;
            this.energyBiasBitmap = null;
            this.pctEnergyMap.Image = null;
            this.pctImage.Image = null;
            txtX.Text = string.Empty;
            txtY.Text = string.Empty;

            this.fileName = string.Empty;
            UpdateTitle();
        }

        private void mnuEditCopyImage_Click(object sender, EventArgs e)
        {
            Clipboard.SetDataObject(this.smImage.CarvedBitmap, true);
        }

        private void btnRevert_Click(object sender, EventArgs e)
        {
            // Reopen the same file
            OpenFile(this.fileName);
        }

        private void mnuAboutSEAMonster_Click(object sender, EventArgs e)
        {
            About about = new About();
            about.ShowDialog(this);
        }

        private void Bitmap_Paint(object sender, PaintEventArgs e)
        {
            // TODO: Graphic artifacts when repeatedly carving...need to investigate
            if (mnuViewEnergyBias.Checked)
            {
                // e.Graphics.PageUnit = GraphicsUnit.Pixel;
                e.Graphics.DrawImage(energyBiasBitmap, 0, 0);
            }
        }

        private void Bitmap_MouseMove(object sender, MouseEventArgs e)
        {
            if (mnuViewEnergyBias.Checked)
            {
                if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
                {
                    Color c;
                    if (e.Button == MouseButtons.Left)
                    {
                        // Left button depends on setting
                        if (lstBiasMode.SelectedItem.ToString() == "Add")
                        {
                            c = Color.FromArgb(127, 0, 255, 0);
                        }
                        else if (lstBiasMode.SelectedItem.ToString() == "Subtract")
                        {
                            c = Color.FromArgb(127, 255, 0, 0);
                        }
                        else
                        {
                            c = Color.FromArgb(0, 0, 0, 0);
                        }
                    }
                    else
                    {
                        // Right button always subtracts
                        c = Color.FromArgb(127, 255, 0, 0);
                    }

                    for (int x = e.X - brushRadius; x < e.X + brushRadius; x++)
                    {
                        for (int y = e.Y - brushRadius; y < e.Y + brushRadius; y++)
                        {
                            if (x >= 0 && x < this.smImage.Size.Width &&
                                y >= 0 && y < this.smImage.Size.Height)
                            {
                                energyBiasBitmap.SetPixel(x, y, c);
                            }
                        }
                    }
                    ((PictureBox)sender).Refresh();
                }
            }
        }
    }
}