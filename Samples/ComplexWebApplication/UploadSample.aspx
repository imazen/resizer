<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="UploadSample.aspx.cs" Inherits="ComplexWebApplication.UploadSample" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    
        <asp:FileUpload ID="fileUpload" runat="server" />
        
        <asp:Button ID="btnUpload" runat="server" Text="Upload, crop and resize!" />
    
    </div>
    </form>
</body>
</html>
