<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="index.aspx.cs" Inherits="ImageResizer.Plugins.Faces.TestWeb.index" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
            <% var dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(this.MapPath("~/"), @"..\TestFaces")); %>
        <%=dir %>
           <% foreach (var f in System.IO.Directory.EnumerateFiles(dir, "*.jpg").Select(f => System.IO.Path.GetFileName(f)))
               { %>
                  <a href="/img/<%=f%>?f.show=true"><img src="/img/<%= f%>?f.show=true&w=100&h=100" width="100" height="100"/></a>
              <%}

            %>
        
    </form>
</body>
</html>
