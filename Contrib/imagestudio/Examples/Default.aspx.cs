using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;

using ImageStudio.Library;

public partial class _Default : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {

    }

    protected void btnSave_OnClick(object sender, EventArgs e)
    {
        if (fuTest.HasFile)
        {
            FileInfo fileInfo = new FileInfo(fuTest.PostedFile.FileName);

            Guid guid = Guid.NewGuid();

            //this is not actualy used, but it could be used for something in the future
            imgstdCropper.fileContentType = fuTest.PostedFile.ContentType;

            //specify web url to the image, where image will be seen by the client
            imgstdCropper.imageUrlPath = @"/Examples/Gallery/" + guid.ToString() + fileInfo.Extension;

            //specify server file path, where file be used for processing and size adjustment
            imgstdCropper.filePath = Server.MapPath(@"Gallery\" + guid.ToString() + fileInfo.Extension);

            //attach actualy fileupload control against the control, image cropper will use posted file to save and then manipulate
            imgstdCropper.fileUpload = fuTest;

            //once everything is ready start cropping of the image
            imgstdCropper.StartCropping();
        }
    }

    protected void imgstdCropper_OnShow(object sender, EventArgs e)
    {
        //If you have other 3rd party controls that you would like to integrate our control with, you can do so by adding Show and Hide to it.
        mpeCropper.Show();
    }

    protected void imgstdCropper_OnHide(object sender, EventArgs e)
    {
        mpeCropper.Hide();
    }

    protected void imgstdCropper_OnSuccesfullyProcessedImage(object sender, EventArgs e)
    {
        try
        {
            //once image has been processed and resized, you can save the image directly, or you can manipulation image furthure by resizing it,
            //after all you need to crop the image with limitations of ratio only to obtain image that you can easily manipulate. 
            //No mater what you are doing i am sure you will find something in this framework.
            FileInfo fileInfo = new FileInfo(imgstdCropper.filePath);
            Guid fileGuid = Guid.NewGuid();
            fileInfo.CopyTo(Server.MapPath(@"Gallery\Ready\" + fileGuid + fileInfo.Extension));


            imgFinal.ImageUrl = "/Examples/Gallery/Ready/" + fileGuid + fileInfo.Extension;
            imgFinal.Visible = true;

            //dont forget, that by the end of this method, the imgstdCropper.filePath file will be deleted! 
            //So you need to move the file or resize it and store it in different folder
        }
        catch (Exception)
        {
            throw new NotImplementedException();
        }
    }

    protected void imgstdCropper_ErrorProcessingImage(object sender, EventArgs e)
    {
        //If there is an error it will be thrown through sender, sender is an Exception object, it will contain relevant message.
        lblError.Text = ((Exception)sender).Message;
    }



}