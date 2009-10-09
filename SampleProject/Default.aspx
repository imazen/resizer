<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="SampleProject._Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Sample page</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <h1>Displaying the same image in different sizes</h1>
    <img src="image.jpg" runat="server" />
    
    <img src="resize(100,100)/image.jpg" runat="server" />
    <img src="~/resize(50,50,png)/image.jpg" runat="server" />
    <br />
        
    <img id="Img2" src="image.jpg?maxwidth=100" runat="server" />
    <img id="Img3" src="image.jpg?maxwidth=50" runat="server" />
    <img id="Img1" src="image.jpg?width=50&height=50" runat="server" />
    <img id="Img4" src="image.jpg?width=50&height=50&crop=auto" runat="server" />
    </div>
    </form>
</body>
</html>
