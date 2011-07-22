<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="RemoteSiteTest.aspx.cs" Inherits="ComplexWebApplication.RemoteSiteTest" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <h2></h2>Testing signed URLs
    <img src='<%=ImageResizer.Plugins.RemoteReader.RemoteReaderPlugin.Current.CreateSignedUrl("http://farm7.static.flickr.com/6021/5959854178_1c2ec6bd77_b.jpg","width=300") %>' />
    <h2>Testing human-friendly URLs and animated gif compatibility with RemoteReaderPlugin</h2>
    <img src="~/remote/images.imageresizing.net/2_computers.gif?width=50" runat="server" />
    </div>
    </form>
</body>
</html>
