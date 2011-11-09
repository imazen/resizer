<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Upload.aspx.cs" Inherits="AzureWebImages.Upload" %><!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
<title></title>
</head>
<body>
<form id="form1" runat="server">
<div>
    Please select a picture to upload.<br />
    <asp:FileUpload ID="fuPicture" runat="server" /> <asp:RequiredFieldValidator ID="rvfUpload" ErrorMessage="Picture required" ControlToValidate="fuPicture" runat="server" />
    <br /><br />
    <asp:Button ID="btnSubmit" Text="Upload" runat="server" onclick="btnSubmit_Click" />
    <br /><br />
    <asp:Literal ID="litImages" runat="server" />
</div>
</form>
</body>
</html>