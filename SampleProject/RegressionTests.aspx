<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="RegressionTests.aspx.cs" Inherits="SampleProject.RegressionTests" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <p>These tests are to make sure that new versions don't re-exhibit old bugs.</p>
    <h3>Using maxwidth and width shouldn't cause aspect ratio problems "?width=600&quality=90&thumbnail=jpg&maxwidth=200"</h3>
    <img src="red-leaf.jpg?width=600&quality=90&thumbnail=jpg&maxwidth=200" runat="server" />
    <h3>Rounding should be the same between the image resizer and GDI. They differ by default.</h3>
    <p>Problem manifests itself as a black line at the bottom of images with troublesome decimal sizes (aspect ratio).</p>
    <img src="rounding-error.jpg?width=150" runat="server" />
    </div>
    </form>
</body>
</html>
