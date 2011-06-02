<%@ Page Language="C#" AutoEventWireup="true"  Inherits="System.Web.UI.Page" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Display images from SQL</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <h1>These images are dynamically loaded from SQL and resized before being sent to the client.</h1>
    <p><a href="~/Upload.aspx" runat="server">Click here to upload photos.</a></p>


    <asp:SqlDataSource 
            id="sql"
          runat="server"
          DataSourceMode="DataReader"
          ConnectionString="<%$ ConnectionStrings:database%>"
          SelectCommand="SELECT ImageID, FileName FROM Images" ></asp:SqlDataSource>

    
    <asp:Repeater DataSourceID="sql" runat="server">
        <ItemTemplate>
        <a href="<%# ResolveUrl("~/databaseimages/" +Eval( "ImageID"))%>">
            <img src="<%# ResolveUrl("~/databaseimages/" + Eval( "ImageID") + "?width=100") %>" alt="<%# "Click for larger view. Original name: "  + Eval( "FileName")%>"/>
            </a>
        </ItemTemplate>
    </asp:Repeater>

    </div>
    </form>
</body>
</html>
