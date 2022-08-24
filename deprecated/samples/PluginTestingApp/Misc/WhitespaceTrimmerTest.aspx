<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="WhitespaceTrimmerTest.aspx.cs" Inherits="ComplexWebApplication.WhitespaceTrimmerTest" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <h1>WhitespaceTrimmer test</h1>
    <p>In the Samples\Images directory, create a subdirectory called 'private' and paste all the images you want to test with WhitespaceTrimmer into that folder.
    <br />You can adjust the whitespace sensitivity and padding in WhitespaceTrimmerTest.aspx.cs</p>
    <p>Original images are displayed on the left, trimmed images on the right</p>
    <asp:Literal ID="lit" runat="server" />
    </div>
    </form>
</body>
</html>
