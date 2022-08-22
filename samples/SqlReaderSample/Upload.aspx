<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Upload.aspx.cs" Inherits="DatabaseSampleCSharp.Upload" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Upload files</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <h2>Select images and files to upload. </h2>
    <p>Images will be resized and cropped to 300x300 and encoded in Jpeg form before being inserted into the database. <a href="~/" runat="server">Click here to view all images</a>.</p>
    <asp:FileUpload runat="server" /> <br />
    <asp:FileUpload ID="FileUpload1" runat="server" /><br />
    <asp:FileUpload ID="FileUpload2" runat="server" /><br />
    <asp:FileUpload ID="FileUpload3" runat="server" /><br />
    <asp:Button Text="Upload" runat="server" onclick="Unnamed2_Click" />
     <asp:Button ID="btnUploadAsIs" Text="Upload As-Is" runat="server" 
            onclick="btnUploadAsIs_Click" />

    </div>
    </form>
</body>
</html>
