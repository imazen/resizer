<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="FriendlyUrlsTest.aspx.cs" Inherits="ComplexWebApplication.FriendlyUrlsTest" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <h3>Should be a 40px jpeg</h3>
       <img src="~/resize(40,40)/red-leaf.jpg" runat="server" />
       <h3>Should be a 40px gif</h3>
       <img id="Img1" src="~/resize(40,40,gif)/red-leaf.jpg" runat="server" />
    </div>
    </form>
</body>
</html>
