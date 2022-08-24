<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Database.aspx.cs" Inherits="CustomOverlaySample._Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <p>Three images. The first is generated using the database, the second using the querystring, and the last is the expected result.</p>
    <div><img src="--Images/ProductSource/2184_74_z.jpg?width=318&mastid=7549&colorid=2218&designid=3303&trim.threshold=50" />

    <img src="Images/ProductSource/2184_74_z.jpg?width=318&customoverlay.coords=791,335,865,325,921,760,848,769&customoverlay.align=topright&customoverlay.magicheight=469&customoverlay.image=CampusCloz_athletics9_light_leftsleeve.png&trim.threshold=50" />

    <img src="Images/FinalOutput/Left Sleeve/2184_74_zCampusCloz_athletics9_light_left sleeve_Left Sleeve.jpg?width=318&trim.threshold=50" />
    </div>

    <p>The remaining are just database vs expected </p>
    <div>
    <img 
src="Images/ProductSource/144NV.png?width=375&height=375&mastid=817&colorid=15&designid=3245&trim.threshold=50" />

<img src="Images/FinalOutput/image007.jpeg" />
<img src="Images/FinalOutput/image008.jpeg" />
</div>

<div>
<img 
src="Images/ProductSource/189OX.png.ashx?width=375&height=375&mastid=1609&colorid=17&designid=3304&trim.threshold=50" />
<img src="Images/FinalOutput/image013.jpeg" />
</div>
<div>
<!--Left Chest - Belvoir - logo should 101px/197px max=51% of width of 
source coordinates; yours is correct for size/position, bur varies in font 
color to mine (grey vs. black) -->

<img 
src="Images/ProductSource/2184_74_z.jpg?width=375&height=375&mastid=7549&colorid=2218&designid=3281&trim.threshold=50" />

<img src="Images/FinalOutput/image004.jpeg" />

</div>

<!--Full Chest - Caribou - logo should be 243px/402px max = 60% of width of 
source coordinates; yours is too small -->
<div>
<img 
src="Images/ProductSource/2184_74_z.jpg?width=375&height=375&mastid=7549&colorid=2218&designid=3279&trim.threshold=50" />

<img src="Images/FinalOutput/image005.jpeg" />
<!--Center Chest - L - logo should be 101px/218px max = 46% of width of 
source coordinates; yours is too small -->
</div>
<div>
<img 
src="Images/ProductSource/2184_74_z.jpg?width=375&height=375&mastid=7549&colorid=2218&designid=3280&trim.threshold=50" />

<img src="Images/FinalOutput/image006.jpeg" />
</div>
<p>And last, a problem with w3wp?</p>

<img src="Images/ProductSource/3311_43_z.jpg.ashx?orgid=2096&mastid=7577&colorid=1445&designid=1&logousageid=3&trim.threshold=50&width=375" />

    </div>
    </form>
</body>
</html>
