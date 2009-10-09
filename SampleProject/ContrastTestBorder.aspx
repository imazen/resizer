<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ContrastTestBorder.aspx.cs" Inherits="SampleProject.ContrastTestBorder" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head id="Head1" runat="server">
    <title>Sample page</title>
</head>
<body style="background-color:Black;">
<form runat="server" id="form1">
<img runat="server" src="red-leaf.jpg?width=300" />
<img id="Img1" runat="server" src="red-leaf.jpg?width=300&bgcolor=black" />
<img id="Img2" runat="server" src="red-leaf.jpg?width=300&bgcolor=green" />
</form>
</body>