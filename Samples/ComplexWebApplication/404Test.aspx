<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="404Test.aspx.cs" Inherits="ComplexWebApplication._404Test" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    There should be a photo of the sun.
    <img src="missing-file.jpg?404=/Sun_256.png" runat="server" />
    </div>
    </form>
</body>
</html>
