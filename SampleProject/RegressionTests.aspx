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
    <img src="rounding-error.png?width=150" runat="server" />
    <h3>Frames after 0 have black instead of transparency</h3>
     <img id="Img1" src="2_computers.gif" runat="server" /><img id="Img2" src="2_computers.gif?frame=4" runat="server" />
     <img id="Img3" src="2_computers.gif?frame=8" runat="server" />
     <img id="Img4" src="2_computers.gif?frame=12" runat="server" />
     <h3>All frames look the same - Some gifs</h3>
     <img src="optical.gif" runat="server" />
      <img id="Img5" src="optical.gif?frame=2" runat="server" />
       <img id="Img6" src="optical.gif?frame=4" runat="server" />
      <h3>Resizing loses transparency</h3>
      <div style="background-color:Yellow;">
          <img id="Img7" src="2_computers.gif" runat="server" /><img id="Img8" src="2_computers.gif?width=20&scale=both" runat="server" />
     <img id="Img9" src="2_computers.gif?frame=8" runat="server" />
     <img id="Img10" src="2_computers.gif?frame=12" runat="server" />
     <img id="Img11" src="clock2.gif" runat="server" />
     <img id="Img12" src="clock2.gif?width=40" runat="server" />
     
          </div>
        <h3>Tiff files should be converted page-by-page</h3>

         <img id="Img15" src="sample.tif?page=1&width=200" runat="server" />
         <img id="Img14" src="sample.tif?page=2&width=200" runat="server" />
         <img id="Img13" src="sample.tif?page=3&width=200" runat="server" />
         
         <h3>There should be a 50% opaque white 1px border on this image. Anything more is a bug</h3>
        
         <div style="background-color:Black; padding:30px">
<img id="Img16" runat="server" src="red-leaf.jpg?width=300" />
</div>

        <h3>This image (5x527) should appear in 3 sizes. 
        </h3>
        <p>Due to GDI bug, the middle one will be a pixel thinner than it should be... But the real bug is if one doesn't appear... then
        we may have an error (dimension < 1px).</p>
 
<img src="horizontal-line.gif" runat="server" />
<img id="Img18" src="horizontal-line.gif?width=200" runat="server" />
<img id="Img17" src="horizontal-line.gif?width=50" runat="server" />

        <h3>Handler Test</h3>
        <img src="HandlerTest.ashx" runat="server" />
        
        
        <h3>IIS Configuration-free mode (.jpg.axd)</h3>
        <img id="Img20" src="rose-leaf.jpg.axd?width=400" runat="server" />
        
        <h3>Security test</h3>
        <img id="Img19" style="border:1px solid black;" src="resize(50,50)/Protected/rose-leaf.jpg" runat="server" />
        <img id="Img21" style="border:1px solid black;" src="Protected/resize(50,50)/rose-leaf.jpg" runat="server" />
        <img id="Img22" style="border:1px solid black;" src="Protected/rose-leaf.jpg.cd?width=50" runat="server" />
        <p>There are three images referenced above... they should not appear.</p>
    </div>
    
  

    </div>
    </form>
</body>
</html>
