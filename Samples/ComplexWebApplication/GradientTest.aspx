<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="GradientTest.aspx.cs" Inherits="ComplexWebApplication.GradientTest" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <style type="text/css">
    body{
    	background:url(gradient.png?width=8&height=8&angle=45&color1=888&color2=white);
    }
    </style>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <img src="gradient.png?width=500&height=300&angle=90&color1=green&color2=ffffff33" />
    </div>
    </form>
</body>
</html>
