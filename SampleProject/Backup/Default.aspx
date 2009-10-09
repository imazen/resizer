<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="SampleProject._Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Untitled Page</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <h1>Displaying the same image in different sizes</h1>
    <img src="image.jpg" runat="server" />
    
    <img src="image.jpg?thumbnail=jpg&width=100" runat="server" />
    <img src="image.jpg?thumbnail=jpg&width=50" runat="server" />
    </div>
    </form>
</body>
</html>
