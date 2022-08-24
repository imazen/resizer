<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="UploadSample.aspx.cs" Inherits="ComplexWebApplication.UploadSample" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <div>
    <a id="A1" href="Default.aspx" runat="server"> Core examples </a> &nbsp;&nbsp;
    <a id="A2"  href="Plugins.htm" runat="server"> Plugin Tests </a> &nbsp;&nbsp;
    <a id="A3"  href="Misc/RegressionTests.aspx" runat="server"> Regression Tests </a> &nbsp;&nbsp;
    <a id="A4" href="UploadSample.aspx" runat="server"> Upload example </a> </div>
        <asp:FileUpload ID="fileUpload" runat="server" />
        
        <asp:Button ID="btnUpload" runat="server" Text="Upload, crop and resize!" />
        
        <asp:Button ID="btnUploadAndGenerate" runat="server" 
            Text="Upload, crop, and resize 3 versions!" 
            onclick="btnUploadAndGenerate_Click" />
    

    </div>
    </form>
</body>
</html>
