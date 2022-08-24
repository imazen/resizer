<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="PsdSampleProject._Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
      <img id="img" src="~/1001.psd.png" runat="server" usemap="#planetmap" />

<map name="planetmap">
<asp:Literal ID="mapdata" runat="server" Mode="PassThrough" />

</map>

    </div>
    </form>
</body>
</html>
