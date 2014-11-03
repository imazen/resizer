<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="default.aspx.cs" Inherits="BasicWebApplication._default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <img src="red-leaf.jpg?width=200" alt="A red leaf." />

    <form id="form1" runat="server">
        <%= System.Reflection.Assembly.GetAssembly(typeof(ImageResizer.Plugins.FastScaling.FastScalingPlugin)).GetName().Version.ToString() %>
     Last built
        <%= (DateTime.UtcNow - new System.IO.FileInfo(System.Reflection.Assembly.GetAssembly(typeof(ImageResizer.Plugins.FastScaling.FastScalingPlugin)).Location).LastWriteTimeUtc).Seconds %>
        seconds ago.
    </form>
</body>
</html>
