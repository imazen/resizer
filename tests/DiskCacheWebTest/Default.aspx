<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs" Inherits="DiskCacheWebTest._Default" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
<script type="text/javascript">
    $(function () {
        for (var i = 1; i < 50; i++) {
            $("<img src='red-leaf.jpg?builder=wic&scache=mem&width=3&rand=" + (i * Math.random()) + "' />").appendTo($(".area"));
            $('.status').text("Generating " + i + " new images, reusing 50 old images");
        }
        for (var i = 1; i < 50; i++) {
            $("<img src='red-leaf.jpg?builder=wic&scache=mem&width=3&nonrand=" + i + "' />").appendTo($(".area"));
        }
    });
</script>
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">

    <br />
    <h3 class="status"></h3>
    <div class="area">
    </div>
    
    <pre><asp:Label  ID="DebugInfo" runat="server" />
    </pre>

    
</asp:Content>
