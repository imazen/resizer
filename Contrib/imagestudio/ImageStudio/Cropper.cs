using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using System.Reflection;

using ImageStudio.Library;

namespace ImageStudio
{
    [ToolboxData("<{0}:Cropper runat=server></{0}:Cropper>")]
    public class Cropper : WebControl, INamingContainer
    {
        #region - Public Event Handlers -
        public event EventHandler SuccesfullyProcessedImage;
        public event EventHandler ErrorProcessingImage;
        public event EventHandler Show;
        public event EventHandler Hide;
        #endregion

        #region - Private Consts -
        private const string KEY_FILENAME = "KEY_FILENAME";
        private const string KEY_CONTENTTYPE = "KEY_CONTENTTYPE";
        #endregion

        #region - Private Properties -
        private int _croppingWidthMax;
        private int _croppingHeightMax;
        private int _croppingWidthMin;
        private int _croppingHeightMin;
        private string _croppingAspectRatio;
        private FileUpload _fileUpload;
        private string _imageUrlPath;
        private string _jqueryExtension = "$";
        private string _onClientCropperImageLoad = string.Empty;
        #endregion

        #region - Public Properties -
        public int croppingWidthMax
        {
            get
            {
                return _croppingWidthMax;
            }
            set
            {
                _croppingWidthMax = value;
            }
        }

        public int croppingHeightMax
        {
            get
            {
                return _croppingHeightMax;
            }
            set
            {
                _croppingHeightMax = value;
            }
        }

        public int croppingWidthMin
        {
            get
            {
                return _croppingWidthMin;
            }
            set
            {
                _croppingWidthMin = value;
            }
        }

        public int croppingHeightMin
        {
            get
            {
                return _croppingHeightMin;
            }
            set
            {
                _croppingHeightMin = value;
            }
        }

        public string croppingAspectRatio
        {
            get
            {
                return _croppingAspectRatio;
            }
            set
            {
                _croppingAspectRatio = value;
            }
        }

        public FileUpload fileUpload
        {
            get
            {
                return _fileUpload;
            }
            set
            {
                _fileUpload = value;
            }
        }

        public string imageUrlPath
        {
            get
            {
                return _imageUrlPath;
            }
            set
            {
                _imageUrlPath = value;
            }
        }

        public string filePath
        {
            get
            {
                return (string)ViewState[KEY_FILENAME];
            }

            set
            {
                ViewState[KEY_FILENAME] = value;
            }
        }

        public string fileExtension
        {
            get
            {
                FileInfo fileInfo = new FileInfo(filePath);
                return fileInfo.Extension;
            }
        }

        public string fileContentType
        {
            get
            {
                return (string)ViewState[KEY_CONTENTTYPE];
            }
            set
            {
                ViewState[KEY_CONTENTTYPE] = value;
            }
        }

        public string JqueryExtension
        {
            get
            {
                return _jqueryExtension;
            }
            set
            {
                _jqueryExtension = value;
            }
        }

        public string OnClientCropperImageLoad
        {
            get
            {
                return _onClientCropperImageLoad;
            }
            set
            {
                _onClientCropperImageLoad = value;
            }
        }
        #endregion

        #region //Protected Controls
        protected Button btnSave = new Button();
        protected Button btnCancel = new Button();

        protected HiddenField hdnFieldY = new HiddenField();
        protected HiddenField hdnFieldX = new HiddenField();
        protected HiddenField hdnFieldWidth = new HiddenField();
        protected HiddenField hdnFieldHeight = new HiddenField();

        protected Image imgEditor = new Image();
        #endregion

        #region //Methods
        /// <summary>
        /// Starts the cropping.
        /// </summary>
        public void StartCropping()
        {
            if (fileUpload != null)
            {
                fileUpload.SaveAs(filePath);

                fileContentType = fileUpload.PostedFile.ContentType;

                //get file resolution
                int widthRaw, heightRaw;
                ImageManipulation.GetResolution(filePath, out widthRaw, out heightRaw);

                if (!(heightRaw < _croppingHeightMin || widthRaw < _croppingWidthMin))
                {

                    if (heightRaw > croppingHeightMax || widthRaw > croppingWidthMax)
                    {
                        if (heightRaw > croppingHeightMax)
                        {
                            ImageManipulation.ResizeWithRatio(filePath, croppingHeightMax, 0);

                            //if just resized, get new dimension and see if we need to resize further.
                            ImageManipulation.GetResolution(filePath, out widthRaw, out heightRaw);
                        }

                        if (widthRaw > croppingWidthMax)
                        {
                            ImageManipulation.ResizeWithRatio(filePath, 0, croppingWidthMax);
                        }
                    }
                    else
                    {
                        //resize file even if it is fine, to avoid corrypted files
                        ImageManipulation.Resize(filePath, heightRaw, widthRaw);
                    }
                    imgEditor.ImageUrl = imageUrlPath;
                    ControlShow();
                }
                else
                {
                    ControlHide();
                    if (ErrorProcessingImage != null)
                    {
                        ErrorProcessingImage(new Exception(String.Format("Image size should be at least {0}x{1}", _croppingWidthMin, _croppingHeightMin)), null);
                    }
                }
            }
        }

        /// <summary>
        /// Controls the show.
        /// </summary>
        public void ControlShow()
        {
            this.Visible = true;
            if (Show != null)
            {
                Show(null, null);
            }
        }

        /// <summary>
        /// Controls the hide.
        /// </summary>
        public void ControlHide()
        {
            this.Visible = false;
            if (Hide != null)
            {
                Hide(null, null);
            }
        }
        #endregion

        #region //Control Events
        /// <summary>
        /// Initializes a new instance of the <see cref="Cropper"/> class.
        /// </summary>
        public Cropper()
        {
            ControlHide();
        }

        /// <summary>
        /// Gets a <see cref="T:System.Web.UI.ControlCollection"/> object that represents the child controls for a specified server control in the UI hierarchy.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// The collection of child controls for the specified server control.
        /// </returns>
        public override ControlCollection Controls
        {
            get
            {
                EnsureChildControls();
                return base.Controls;
            }
        }

        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based implementation to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        protected override void CreateChildControls()
        {
            Controls.Clear();

            btnSave.Text = "Save";
            btnSave.CssClass = "ButtonSave";
            btnSave.Click += new EventHandler(btnSave_OnClick);
            Controls.Add(btnSave);

            btnCancel.Text = "Cancel";
            btnCancel.CssClass = "ButtonCancel";
            btnCancel.Click += new EventHandler(btnCancel_OnClick);
            Controls.Add(btnCancel);

            Controls.Add(hdnFieldHeight);
            Controls.Add(hdnFieldWidth);
            Controls.Add(hdnFieldX);
            Controls.Add(hdnFieldY);
            Controls.Add(imgEditor);
        }

        /// <summary>
        /// Handles the OnClick event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void btnSave_OnClick(object sender, EventArgs e)
        {
            try
            {
                int x = int.Parse(hdnFieldX.Value);
                int y = int.Parse(hdnFieldY.Value);

                int width = int.Parse(hdnFieldWidth.Value);
                int height = int.Parse(hdnFieldHeight.Value);

                if (ImageManipulation.Crop(filePath, x, y, width, height))
                {
                    if (SuccesfullyProcessedImage != null)
                    {
                        SuccesfullyProcessedImage(sender, e);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ErrorProcessingImage != null)
                {
                    //ErrorProcessingImage(ex, e);
                    ErrorProcessingImage(new Exception("Please make sure that you have cropped your image"), e);
                }
            }
            finally
            {
                FileInfo fileInfo = new FileInfo(filePath);

                if (fileInfo.Exists)
                {
                    fileInfo.Delete();
                    ControlHide();
                }
            }
        }

        /// <summary>
        /// Handles the OnClick event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void btnCancel_OnClick(object sender, EventArgs e)
        {
            if (filePath.Length > 0)
            {
                FileInfo fileInfo = new FileInfo(filePath);
                if (fileInfo.Exists)
                {
                    fileInfo.Delete();
                    ControlHide();
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.PreRender"/> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs"/> object that contains the event data.</param>
        protected override void OnPreRender(EventArgs e)
        {
            //get embed resource (javascript) and add it
            if (!Page.ClientScript.IsClientScriptIncludeRegistered("ImageStudio.Resouces.Cropper.jquery.Jcrop.min.js"))
            {
                
                Page.ClientScript.RegisterClientScriptResource(this.GetType(), "ImageStudio.Resouces.Cropper.jquery.Jcrop.min.js");
            }


            Assembly assembly = Assembly.GetExecutingAssembly();

            if (!Page.ClientScript.IsClientScriptBlockRegistered("ImageStudio.Cropper.Load"))
            {
                StringBuilder sbJavaScript = new StringBuilder();
                using (StreamReader streamReader = new StreamReader(assembly.GetManifestResourceStream("ImageStudio.Resouces.Cropper.jquery.Load.js")))
                {
                    sbJavaScript.Append("<script language=\"javascript\" type=\"text/javascript\">");
                    sbJavaScript.Append(streamReader.ReadToEnd());
                    sbJavaScript.Replace("<ClientID>", ClientID);
                    sbJavaScript.Replace("<Image.ClientID>", imgEditor.ClientID);
                    sbJavaScript.Replace("<JQExtension>", _jqueryExtension);
                    sbJavaScript.Replace("<CroppingAspectRatio>", croppingAspectRatio);
                    sbJavaScript.Replace("<CroppingWidthMin>", croppingWidthMin.ToString());
                    sbJavaScript.Replace("<CroppingHeightMin>", croppingHeightMin.ToString());
                    sbJavaScript.Replace("<FieldX.ClientID>", hdnFieldX.ClientID);
                    sbJavaScript.Replace("<FieldY.ClientID>", hdnFieldY.ClientID);
                    sbJavaScript.Replace("<FieldWidth.ClientID>", hdnFieldWidth.ClientID);
                    sbJavaScript.Replace("<FieldHeight.ClientID>", hdnFieldHeight.ClientID);
                    sbJavaScript.Replace("<OnClientCropperImageLoad>", _onClientCropperImageLoad);
                    sbJavaScript.Append("</script>");
                }

                Page.ClientScript.RegisterClientScriptBlock(this.GetType(), "ImageStudio.Cropper.Load", sbJavaScript.ToString());
            }

            //style sheet setup
            StringBuilder sbStyleSheet = new StringBuilder();
            using (StreamReader streamReader = new StreamReader(assembly.GetManifestResourceStream("ImageStudio.Resouces.Cropper.jquery.Jcrop.css")))
            {
                sbStyleSheet.Append("<style>");
                sbStyleSheet.Append(streamReader.ReadToEnd());
                sbStyleSheet.Replace("<image>", Page.ClientScript.GetWebResourceUrl(this.GetType(), "ImageStudio.Resouces.Cropper.Jcrop.gif"));
                sbStyleSheet.Append("</style>");
            }
            Page.Header.Controls.Add(new LiteralControl(sbStyleSheet.ToString()));

            base.OnPreRender(e);
        }

        /// <summary>
        /// Renders the contents.
        /// </summary>
        /// <param name="output">The output.</param>
        protected override void RenderContents(HtmlTextWriter output)
        {
            output.RenderBeginTag(HtmlTextWriterTag.Div);
            output.RenderBeginTag(HtmlTextWriterTag.Div);
            output.RenderBeginTag(HtmlTextWriterTag.Div);
            hdnFieldHeight.RenderControl(output);
            hdnFieldWidth.RenderControl(output);
            hdnFieldX.RenderControl(output);
            hdnFieldY.RenderControl(output);
            imgEditor.RenderControl(output);
            output.RenderEndTag();
            btnSave.RenderControl(output);
            btnCancel.RenderControl(output);
            output.RenderEndTag();
            output.RenderEndTag();
        }
        #endregion
    }
}
