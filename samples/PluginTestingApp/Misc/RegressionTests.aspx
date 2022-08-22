<%@ Page Language="C#" AutoEventWireup="true"  %>
<%@ Import Namespace="ImageResizer.Plugins.RemoteReader" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <div>
    <a id="A1" href="../Default.aspx" runat="server"> Core examples </a> &nbsp;&nbsp;
    <a id="A2"  href="../Plugins.htm" runat="server"> Plugin Tests </a> &nbsp;&nbsp;
    <a id="A3"  href="RegressionTests.aspx" runat="server"> Regression Tests </a> &nbsp;&nbsp;
    <a id="A4" href="../UploadSample.aspx" runat="server"> Upload example </a> </div>

    <p>These tests are to make sure that new versions don't re-exhibit old bugs.</p>
    <h3>Using maxwidth and width shouldn't cause aspect ratio problems "?width=600&quality=90&thumbnail=jpg&maxwidth=200"</h3>
    <img src="red-leaf.jpg?width=600&quality=90&thumbnail=jpg&maxwidth=200" runat="server" />
    <h3>Rounding should be the same between the image resizer and GDI. They differ by default.</h3>
    <p>Problem manifests itself as a black line at the bottom of images with troublesome decimal sizes (aspect ratio).</p>
    <img src="rounding-error.png?width=150" runat="server" />
    <h3>Frames after 0 have black instead of transparency</h3>
     <img src="2_computers.gif" runat="server" /><img src="2_computers.gif?frame=4" runat="server" />
     <img src="2_computers.gif?frame=8" runat="server" />
     <img src="2_computers.gif?frame=12" runat="server" />
     <h3>All frames look the same - Some gifs</h3>
     <img src="optical.gif" runat="server" />
      <img src="optical.gif?frame=2" runat="server" />
       <img src="optical.gif?frame=4" runat="server" />
       
      <h3>Resizing loses transparency</h3>
      <div style="background-color:Yellow;">
     <img src="2_computers.gif" runat="server" /><img src="2_computers.gif?width=20&scale=both" runat="server" />
     <img src="2_computers.gif?frame=8" runat="server" />
     <img src="2_computers.gif?frame=12" runat="server" />
     <img src="clock2.gif" runat="server" />
     <img src="clock2.gif?width=40" runat="server" />
     
          </div>
        <h3>Tiff files should be converted page-by-page</h3>

         <img src="sample.tif?page=1&width=200" runat="server" />
         <img src="sample.tif?page=2&width=200" runat="server" />
         <img src="sample.tif?page=3&width=200" runat="server" />

        <h3>Tiff files should be converted page-by-page (using FreeImage decoder)</h3>

         <img src="sample.tif?page=1&decoder=freeimage&width=200" runat="server" />
         <img src="sample.tif?page=2&decoder=freeimage&width=200" runat="server" />
         <img src="sample.tif?page=3&decoder=freeimage&width=200" runat="server" />
         
         <h3>There should not be any kind of border on these images.</h3>
        
         <div style="background-color:Black; padding:30px">
<img runat="server" src="red-leaf.jpg?width=300" /><img id="Img2" runat="server" src="red-leaf.jpg?width=300&builder=wic" />
<img id="Img3" runat="server" src="red-leaf.jpg?width=300&builder=freeimage" />
</div>

<h3>No clipping should occur (original, followed by resized)</h3>
<div style="background-color:white; padding:30px">
<img src="rounding-error-2.jpg" runat="server" />
<br />
<img src="rounding-error-2.jpg?maxheight=50" runat="server" />
</div>
        <h3>This image (5x527) should appear in 3 sizes. 
        </h3>
        <p>Due to GDI bug, the middle one will be a pixel thinner than it should be... But the real bug is if one doesn't appear... then
        we may have an error (dimension < 1px).</p>
 
<img src="horizontal-line.gif" runat="server" />
<img src="horizontal-line.gif?width=200" runat="server" />
<img src="horizontal-line.gif?width=50" runat="server" />

        <h3>Handler Test</h3>
        <img src="HandlerTest.ashx" runat="server" />
        
        
        <h3>IIS Configuration-free mode (.jpg.ashx)</h3>
        <img src="rose-leaf.jpg.ashx?width=400" runat="server" />
        
  

        <h3> More rounding bugs. </h3>

        There should not be a 1px white line inside the bottom border:
        <img src="red-leaf.jpg?width=250&borderWidth=40&borderColor=green" />

        <h2>Testing signed URLs</h2>
    <img src='<%= RemoteReaderPlugin.Current.CreateSignedUrl("http://farm7.static.flickr.com/6021/5959854178_1c2ec6bd77_b.jpg", "width=300") %>' />
    <h2>Testing human-friendly URLs and animated gif compatibility with RemoteReaderPlugin</h2>
    <img id="Img1" src="~/remote/img.imageresizing.net/2_computers.gif?width=50" runat="server" />

    </div>
    
  

    </form>
</body>
</html>
