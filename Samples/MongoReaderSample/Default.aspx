<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="MongoReaderSample.Upload" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <h1>Upload, resize, and store in GridFS</h1>
            <asp:FileUpload ID="fileUpload" runat="server" />
        
        <asp:Button ID="btnUpload" runat="server" Text="Upload, resize, store, & view!" />
        
        <asp:Literal ID="lit" runat="server" />
    </div>
    </form>
</body>
</html>
