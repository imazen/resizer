<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ListImages.aspx.cs" Inherits="ComplexWebApplication.ListImages" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    
<script type="text/javascript" src="js/jquery-1.6.2.min.js"></script>
<script type="text/javascript">
    //<!--

    $(function () {
        $("#images img").mousedown(function (evt) {
            if (evt.which == 2) {
            }
            if (evt.which == 1) {
                var offset = $(this).offset();
                this.downx = evt.offsetX;
                this.downy = evt.offsetY;
                evt.preventDefault();
            }
        });

        $("#images img").mouseup(function (evt) {
            if (evt.which == 1) {
                var radius = Math.sqrt(Math.pow(evt.offsetX - this.downx,2) + Math.pow(evt.offsetY - this.downy,2));

                var segment = this.downx + "," + this.downy + "," + radius + ",";

                var key = "&r.eyes=";
                var qs = $(this).attr("src");
                if (qs.indexOf(key) < 0) qs += key + segment;
                else {
                    qs = qs.replace(/\&r\.eyes\=/i, key + segment);
                }
                $(this).attr("src", qs);
            }
        });


    });
//-->
</script>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    The folder to enumerate: <br />
    <asp:TextBox ID="folder" Text="private/redeye" runat="server" 
            /><br />
    Enter multiple querystrings to compare multiple versions of the images<br />
    <asp:TextBox ID="col1" Text="width=200;" runat="server" Width="400" /><br />
    <asp:TextBox ID="col2" Text="width=200;r.filter=0;r.sobel=true;" runat="server"  Width="400"/><br />
    <asp:TextBox ID="col3" Text="" runat="server"  Width="400"/><br />
    <asp:TextBox ID="col4" Text="" runat="server"  Width="400"/><br />
    <asp:TextBox ID="col5" Text="" runat="server"  Width="400"/><br />
    <asp:TextBox ID="col6" Text="" runat="server"  Width="400"/><br />
    <asp:TextBox ID="col7" Text="" runat="server"  Width="400"/><br />
    <asp:TextBox ID="col8" Text="" runat="server"  Width="400"/><br />
    <asp:Button ID="show" Text="Show Images" runat="server" onclick="show_Click" />

    <div id="images">
    <asp:Literal ID="lit" runat="server" />
    </div>
    </div>
    </form>
</body>
</html>
