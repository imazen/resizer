<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="TestLimits.aspx.cs" Inherits="ComplexWebApplication.TestLimits" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <h1>SizeLimiting plugin tests</h1>
    The following request should result in an 800x600 or smaller photo: <a href="quality-original.jpg?width=1000">click here</a>.
    <br />The following request should throw an error: <a href="quality-original.jpg?width=1000&paddingWidth=5000">click here</a>.
    </div>
    </form>
</body>
</html>
