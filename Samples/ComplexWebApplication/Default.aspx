<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="SampleProject._Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Sample page</title>
	
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <a href="~/Default.aspx" runat="server"> Core examples </a> &nbsp;&nbsp;
    <a  href="~/Plugins.htm" runat="server"> Plugin Tests </a> &nbsp;&nbsp;
    <a  href="~/Misc/RegressionTests.aspx" runat="server"> Regression Tests </a> &nbsp;&nbsp;
    <a href="~/UploadSample.aspx" runat="server"> Upload example </a> </div>
    <div>
    <h1>Examples</h1>
    <p>Hover over the images to view the querystrings used. All commands can be combined.</p>
    
		<div class="toggle">
        <div>
    <h3>Resizing using maxwidth and/or maxheight</h3>
    <p>Aspect ratio is always maintained with maxwidth and maxheight. The image is scaled to fit within those bounds.</p>
    <img src="red-leaf.jpg?maxwidth=300" runat="server" title="With ?maxwidth=300"/>
    <img  src="grass.jpg?maxheight=300" runat="server" title="With ?maxheight=300"/>
    <img  src="tulip-leaf.jpg?maxwidth=300&amp;maxheight=300" runat="server" title="With ?maxwidth=300&amp;maxheight=300"/>
	</div>
	</div>
    <h2>Resizing using width and height</h2>
    <p>Specifying only one of <em>width</em> or <em>height</em> will behave the same as using <em>maxwidth</em>
    or <em>maxheight</em>. The difference is when you specify both.</p>
    <p>Specifying both width and height will force the image to those exact dimensions, unless the 
    image is already smaller (see scale). This is done by adding whitespace to the image. To center and crop instead, use
    <strong>&amp;crop=auto</strong>. To lose aspect ratio and fill the specified rectangle, use <strong>&amp;stretch=fill</strong>.</p>
    <img style="border:1px solid gray;" src="grass.jpg?width=200&height=200" runat="server"
     title="Shown with a border so you can see the added whitespace. ?width=200&height=200"/>
     <img src="grass.jpg?width=200&height=200&bgcolor=black" runat="server"
      title="With ?width=200&height=200&bgcolor=black"/>
    <img  src="red-leaf.jpg?width=100&height=200&stretch=fill" runat="server" 
    title="Distorted to 100x200. ?width=100&height=200&stretch=fill"/>
    <img src="red-leaf.jpg?width=100&height=200&crop=auto" runat="server" 
    title="Cropped to 100x200. ?width=100&height=200&crop=auto"/>
    
    <h2>Scaling</h2>
    <p>By default, images are not upscaled. If an image is already smaller than width/height/maxwidth/maxheight, it is not resized.
    To upscale images, use <strong>?scale=both</strong>. <strong>?scale=downscaleonly</strong> is the default.</p>
    
    
    <img src="tractor-tiny.jpg?scale=both&width=200" runat="server" 
    title="Upscaled from 100px to 200px using ?scale=both&width=200" />
    <br />
    The slight blur around the edges is a bug in Graphics.DrawImage(). You can control the color by setting <strong>&amp;bgcolor=color|hex</strong>.
    
    <p>Upscaling the canvas is sometimes desired instead of upscaling the image when it is smaller than the requested size. Use <strong>?scale=upscalecanvas</strong> to achieve this effect.</p>
    <img src="tractor-tiny.jpg?scale=upscalecanvas&width=200&borderColor=black&borderWidth=2" runat="server" 
    title="?scale=upscalecanvas&width=200&borderColor=black&borderWidth=2" />
    
    
    <h2>Cropping</h2>
    <p>To enable cropping, you can use <strong>&amp;crop=auto</strong>, which minimally crops and centers to preserve aspect ratio, or custom cropping.</p>
    <p><strong>&amp;crop=(x1,y1,x2,y2)</strong> specifies the rectangle to crop on the image. You can still resize and modify the cropped portion 
    using the other commands as normal. Negative coordinates are relative to the bottom-right corner - 
    which makes it easy to trim off a 50-pixel border by specifying <strong>&amp;crop=(50,50,-50,-50)</strong>.</p>
    
    <img src="tractor.jpg?crop=(10,150,200,350)" runat="server" 
    title="Cropping out a 200x200 square using ?crop=(10,150,200,350)"
    />
    <img src="tractor.jpg?crop=(60,200,250,400)" runat="server" 
    title="Cropping out a 200x200 square using ?crop=(60,200,250,400)"
    />
    <img src="tractor.jpg?crop=auto&width=300&height=150" runat="server" 
    title="Cropping out to 300x150 square using ?crop=auto&width=300&height=150"
    />
    <p>Cropping can also be done against arbitrary scales, which is very useful for jQuery jCrop interfaces. 
    Example to crop 10% off each edge: crop=(.1,.1,.9,.9)&cropxunits=1&cropyunits=1 
    Example to crop an image relative to the 'final' coordinates, without knowing the original size. Ex. crop=(20,30,400,350)&cropxunits=500&cropxunits=390</p>
    <img src="tractor.jpg?width=200" runat="server" 
    title="original"
    /> <img src="tractor.jpg?crop=(.1,.1,.9,.9)&cropxunits=1&cropyunits=1&width=200" runat="server" 
    title="Cropping 10% off each edge using ?crop=(.1,.1,.9,.9)&cropxunits=1&cropyunits=1"
    />
    <h2>Rotation</h2>
    <p>Rotation is easy - just specify the number of degrees. You may want to set bgcolor also.</p>
    <img src="red-leaf.jpg?rotate=30&maxwidth=100" runat="server" 
    
    title="With ?rotate=30&maxwidth=100"/>
    
    <img src="tractor.jpg?rotate=-30&crop=(60,200,250,400)" runat="server" 
    title="Using ?rotate=-30&amp;crop=(60,200,250,400)"
    />
    
    <img src="grass.jpg?rotate=45&width=200&height=200&crop=auto&bgcolor=black" runat="server"
      title="With ?rotate=45&width=200&height=200&crop=auto&bgcolor=black"/>
      
    <h2>Flipping</h2>
    <p>You can horizontally or verically flip an image, as well as both. <strong>&amp;flip=h|v|both</strong></p>
    <img src="tractor.jpg?flip=v&crop=(60,200,250,400)" runat="server" 
    title="Using ?flip=v&crop=(60,200,250,400)"
    />
     <img src="tractor.jpg?flip=both&crop=(60,200,250,400)" runat="server" 
    title="Using ?flip=both&crop=(60,200,250,400)"
    /><br />
    <img src="tractor.jpg?crop=(60,200,250,400)" runat="server" 
    title="Using ?crop=(60,200,250,400)"
    />
     <img src="tractor.jpg?flip=h&crop=(60,200,250,400)" runat="server" 
    title="Using ?flip=h&crop=(60,200,250,400)"
    />
    <h2>Source flipping</h2>
    <p>Since normal flipping applies after rotation and cropping occur, it can be 
    difficult to work with if you are just wanting the source image flipped before the other
    adjustments are applied. To flip the source prior to work, use <strong>&amp;sourceFlip=h|v|both</strong>.</p>
    <p>Note how the same crop coordinates return different sections of the image. This is because the source image is flipped before *anything* happens.</p>
    <img src="tractor.jpg?maxwidth=100" runat="server" 
    title="Using ?maxwidth=100"
    />
    <img src="tractor.jpg?crop=(0,0,100,100)" runat="server" 
    title="Using ?crop=(0,0,100,100)"
    />
    <img src="tractor.jpg?flip=both&crop=(0,0,100,100)" runat="server" 
    title="Using ?flip=both&crop=(0,0,100,100)"
    />
    <h2>Stretching</h2>
    <p>To stretch an image to width and height, use <strong>&amp;stretch=fill</strong>. </p>
    <img src="tractor.jpg?stretch=fill&width=200&height=100" runat="server" 
    title="Using ?stretch=fill&width=200&height=100)"
    />
    <h2>Padding</h2>
    <p>You can add padding around the image with <strong>&amp;paddingWidth=px</strong> and 
    <strong>&amp;paddingColor=color|hex</strong>. paddingColor defaults to bgcolor, which defaults to white.
    This setting can be.... useful.</p>
    <img src="grass.jpg?maxwidth=200&paddingColor=black&paddingwidth=20" runat="server"
      title="With ?maxwidth=200&paddingColor=black&paddingwidth=20"/>
    <h2>Borders</h2>
    <p>You can add a border around the image with <strong>&amp;borderWidth=px</strong>, 
    <strong>&amp;borderColor=color|hex</strong>.</p>
    <img src="grass.jpg?maxwidth=200&paddingColor=white&paddingwidth=20&borderWidth=8&borderColor=808080" runat="server"
      title="With ?maxwidth=200&borderWidth=8&borderColor=808080&paddingwidth=20"/>
    
    
    
    <h1>Output format</h1>
    <p>GIF, JPG, and PNG output is supported. BMP and TIFF input fils are additionally supported, and every format can be converted to any other format with <strong>&amp;format=jpg|png|gif</strong></p>
    <h2>Jpeg compression levels 0-100 (&amp;quality=0-100)</h2>
    <p>The sizes of these images range from 855B to 12KB. The largest size jump is from 90 to 100 (5KB to 12KB). I think 90 is a good balance, and is therefore the default.</p>
     <img src="tulip-leaf.jpg?quality=0&maxwidth=100" runat="server" />
    <img src="tulip-leaf.jpg?quality=10&maxwidth=100" runat="server" />
    <img src="tulip-leaf.jpg?quality=20&maxwidth=100" runat="server" />
    <img src="tulip-leaf.jpg?quality=30&maxwidth=100" runat="server" />
    <img src="tulip-leaf.jpg?quality=40&maxwidth=100" runat="server" />
    <img src="tulip-leaf.jpg?quality=50&maxwidth=100" runat="server" />
    <img src="tulip-leaf.jpg?quality=60&maxwidth=100" runat="server" />
    <img src="tulip-leaf.jpg?quality=70&maxwidth=100" runat="server" />
    <img src="tulip-leaf.jpg?quality=80&maxwidth=100" runat="server" />
    <img src="tulip-leaf.jpg?quality=90&maxwidth=100" runat="server" />
    <img src="tulip-leaf.jpg?quality=100&maxwidth=100" runat="server" />
    <h2>Quantization (8-bit PNG and GIFs)</h2>
    <p>The default GDI quantization is terrible, and produces lousy GIFs. Using the Octree quantizer, you can 
    control the number of colors (and therefore the size and quality) of an image using <strong>&amp;colors=2-255</strong></p>
    <h3>GIFs, in 4,8,16,32,64,128, and 256 colors</h3>
    <img src="tractor.jpg?&maxwidth=100&format=gif&colors=4" runat="server" />
    <img src="tractor.jpg?&maxwidth=100&format=gif&colors=8" runat="server" />
    <img src="tractor.jpg?&maxwidth=100&format=gif&colors=16" runat="server" />
    <img src="tractor.jpg?&maxwidth=100&format=gif&colors=32" runat="server" />
    <img src="tractor.jpg?&maxwidth=100&format=gif&colors=64" runat="server" />
    <img src="tractor.jpg?&maxwidth=100&format=gif&colors=128" runat="server" />
    <img src="tractor.jpg?&maxwidth=100&format=gif&colors=256" runat="server" />
    <h3>PNGs, in 4,8,16,32,64,128, 256, and 16 million colors</h3>
        <img src="tractor.jpg?&maxwidth=100&format=png&colors=4" runat="server" />
    <img src="tractor.jpg?&maxwidth=100&format=png&colors=8" runat="server" />
    <img src="tractor.jpg?&maxwidth=100&format=png&colors=16" runat="server" />
    <img src="tractor.jpg?&maxwidth=100&format=png&colors=32" runat="server" />
    <img src="tractor.jpg?&maxwidth=100&format=png&colors=64" runat="server" />
    <img src="tractor.jpg?&maxwidth=100&format=png&colors=128" runat="server" />
    <img src="tractor.jpg?&maxwidth=100&format=png&colors=256" runat="server" />
    <img src="tractor.jpg?&maxwidth=100&format=png" runat="server" />
    <h2>Transparent GIFs and PNGs</h2>
    <h3>Transparency is maintained when resizing PNGs</h3>
    <img src="sun_256.png" runat="server" />
    <img src="sun_256.png?maxwidth=100" runat="server" />
    <img style="background-color:black" src="sun_256.png?&maxwidth=200" runat="server" />
    <h3>Matte backgrounds can be applied with bgcolor. </h3>
    <img src="moon_256.png?maxwidth=100&bgcolor=red" runat="server" />
    <img src="moon_256.png?maxwidth=100&bgcolor=blue" runat="server" />
    <img src="moon_256.png?maxwidth=100&bgcolor=green" runat="server" />
    <h3>Transparent PNGs can be converted to transparent GIFs</h3>
    <p>It's ugly, but GIFs only get 1 bit for transparency.</p>
    <img src="saturn_256.png?format=gif" runat="server" />

     <h3>Transparency is maintained while resizing GIFs</h3>
     <p>Here we upscale a normally tiny GIF</p>
     <img src="2_computers.gif?width=300&scale=both" runat="server" />
     
    <br />
    
    
    </div>
    </form>

</body>
</html>
