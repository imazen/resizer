<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Watermarks.aspx.cs" Inherits="ComplexWebApplication.Watermarks" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <h2>This should have a watermark of a sun</h2>
    <img src="quality-original.jpg?width=400&watermark=Sun_256.png" />
    <h2>This should not, since the watermark file doesn't exist</h2>
    <img src="quality-original.jpg?width=400&watermark=Sun_256-missing.png" />
    </div>
    </form>
</body>
</html>
