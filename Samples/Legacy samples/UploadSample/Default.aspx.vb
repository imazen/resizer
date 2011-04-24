Imports fbs.ImageResizer
Imports System.Drawing.Imaging

Partial Class _Default
    Inherits System.Web.UI.Page




    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

    End Sub


    Protected Sub Upload_Clicked(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnSubmit.Click
        Dim uploadFolder As String = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath + "\uploads\" '"C:\images\"
        Dim settings As String = "?maxwidth=1920&maxheight=1200"

        If (Not fUpload.PostedFile Is Nothing) Then
            Dim newName As String = System.Guid.NewGuid().ToString() + System.IO.Path.GetExtension(fUpload.PostedFile.FileName)
            Dim newPath As String = uploadFolder + newName

            fbs.ImageResizer.ImageManager.getBestInstance().BuildImage(fUpload.PostedFile, newPath, New fbs.yrl(settings).QueryString)
        End If

    End Sub


End Class
