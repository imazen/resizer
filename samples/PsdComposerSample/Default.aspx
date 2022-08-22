<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="PsdComposerSample.Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <h3>To change a layer's text, add the layer name and replacement text pair to the page querystring in ?layername=newtext form</h3>
    <asp:Image ID="img" runat="server" />
    <h3>List of layer names and values</h3>
    <asp:Literal runat="server" ID="lit" />
    </div>
    </form>
</body>
</html>
