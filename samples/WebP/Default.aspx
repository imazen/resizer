<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="WebP._Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <style type="text/css">
        h6{ margin:0; padding:0; font-size:11pt; font-family:Sans-Serif;}
    </style>
</head>
<body>
    <form id="form1" runat="server">
    <div>

    <h1>Visual comparison of JPEG/PNG and WebP encoding</h1>
    <p>Note that the jpeg and webp encoders interpret the 'quality' setting differently. In order to horizontally line up the closest 'visual quality' images, we are using a custom mapping of jpeg->webp quality values.</p>
    <table>
    <th><td>Jpeg/PNG</td><td>WebP</td></th>

    <asp:Literal ID="lit" runat="server" Mode=PassThrough />
    </table>
    
    </div>
    </form>
</body>
</html>
