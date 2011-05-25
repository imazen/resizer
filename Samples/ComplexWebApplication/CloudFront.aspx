<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="CloudFront.aspx.cs" Inherits="ComplexWebApplication.CloudFront" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <p>CloudFront removes all querystrings. The CloudFront plugin allows querystrings to bypass the querystring guillotine by using ';' instead of '?' and '&'. </p>
    <img src="red-leaf.jpg;width=100;height=200" />
    </div>
    </form>
</body>
</html>
