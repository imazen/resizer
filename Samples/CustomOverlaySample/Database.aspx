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
    <img src="Images/ProductSource/2184_74_z.jpg?width=318&mastid=7549&colorid=2218&designid=3303&trim.threshold=50" />

    <img src="Images/ProductSource/2184_74_z.jpg?width=318&customoverlay.coords=791,335,865,325,921,760,848,769&customoverlay.align=topright&customoverlay.magicheight=469&customoverlay.image=CampusCloz_athletics9_light_leftsleeve.png&trim.threshold=50" />

    <img src="Images/FinalOutput/Left Sleeve/2184_74_zCampusCloz_athletics9_light_left sleeve_Left Sleeve.jpg?width=318&trim.threshold=50" />
    </div>
    </form>
</body>
</html>
