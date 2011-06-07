<%@ Page Language="C#" AutoEventWireup="true"  Inherits="System.Web.UI.Page" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<script runat="server">

    protected void btnClear_Click(object sender, EventArgs e) {
        sql.Delete();
    }
</script>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Display images from SQL</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <h1>These images are dynamically loaded from SQL.</h1>
    <p> <a href="Upload.aspx">Click here to upload photos and files.</a></p>


    <asp:SqlDataSource 
            id="sql"
          runat="server"
          DataSourceMode="DataReader"
          ConnectionString="<%$ ConnectionStrings:database%>"
          SelectCommand="SELECT DocKey, DocFnm FROM Documents" 
           DeleteCommand="TRUNCATE TABLE Documents;
 DBCC SHRINKFILE (2, 1); DBCC SHRINKDATABASE (0,1)"></asp:SqlDataSource>

    
    <asp:Repeater DataSourceID="sql" runat="server">
        <ItemTemplate>
        <a href="<%# ResolveUrl("~/databaseimages/" + Eval("DocKey") + "/" + Eval("DocFnm"))%>">
            <img src="<%# ImageResizer.Configuration.Config.Current.Pipeline.IsAcceptedImageType((string)Eval("DocFnm")) ?
               (ResolveUrl("~/databaseimages/" + Eval("DocKey") + "/" + Eval("DocFnm") + "?width=100")) : "/file.png" %>" alt="<%# "Click for larger view. Original name: "  + Eval( "DocFnm")%>"/>
            </a>
        </ItemTemplate>
    </asp:Repeater>

    <asp:Button ID="btnClear" Text="Remove all images" runat="server" 
            onclick="btnClear_Click" />

    </div>
    </form>
</body>
</html>
