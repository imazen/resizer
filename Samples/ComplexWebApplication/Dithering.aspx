<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Dithering.aspx.cs" Inherits="SampleProject.Dithering" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Dithering tests</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <h3>Without dither</h3>
    <img id="Img4" src="red-leaf.jpg?format=gif" runat="server" />
    <h3>With dither (Floyd-Steinberg  30%)</h3>
    <img id="Img1" src="red-leaf.jpg?format=gif&dither=true" runat="server" />
    <h3>With 4-pass dither (Floyd-Steinberg  30%)</h3>
    <img id="Img2" src="red-leaf.jpg?format=gif&dither=4pass" runat="server" />
    
    <h3>Without dither</h3>
    <img id="Img7" src="red-leaf.jpg?format=gif" runat="server" />
    <h3>20% dither</h3>
    <img id="Img6" src="red-leaf.jpg?format=gif&dither=20" runat="server" />
    <h3>50% dither</h3>
    <img id="Img3" src="red-leaf.jpg?format=gif&dither=50" runat="server" />
    <h3>75% dither</h3>
    <img id="Img5" src="red-leaf.jpg?format=gif&dither=75" runat="server" />
    </div>
    </form>
</body>
</html>
