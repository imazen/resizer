<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Upload.aspx.cs" Inherits="S3ReaderSample.Upload" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>

        <h3>All fields are require to perform an upload</h3>

        AWS ID: <input type="password"
        name="awsid" id="awsid" /><br />
        AWS SECRET: <input type="password"
        name="awssecret" id="awssecret" /><br />

        AWS Bucket: <asp:TextBox ID="bucket" runat="server" />
        <br />

        <asp:FileUpload ID="upload" runat="server" />

        
        <br />

        <asp:Label ID="result" runat ="server" />
        <asp:Button ID="submit" UseSubmitBehavior="true" runat="server" />
    </div>
    </form>
</body>
</html>
