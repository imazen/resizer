<%@ Page Language="C#" AutoEventWireup="true"  %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <div>
    <a id="A1" href="../Default.aspx" runat="server"> Core examples </a> &nbsp;&nbsp;
    <a id="A2"  href="../Plugins.htm" runat="server"> Plugin Tests </a> &nbsp;&nbsp;
    <a id="A3"  href="RegressionTests.aspx" runat="server"> Regression Tests </a> &nbsp;&nbsp;
    <a id="A4" href="../UploadSample.aspx" runat="server"> Upload example </a> </div>

        
        
        <h3>Security test</h3>
        <img style="border:1px solid black;" src="resize(50,50)/Protected/rose-leaf.jpg" runat="server" />
        <img style="border:1px solid black;" src="Protected/resize(50,50)/rose-leaf.jpg" runat="server" />
        <img style="border:1px solid black;" src="Protected/rose-leaf.jpg.cd?width=50" runat="server" />
                <img style="border:1px solid black;" src="resize(50,50)/Protected/rose-leaf2.jpg" runat="server" />
        <img  style="border:1px solid black;" src="Protected2/resize(50,50)/rose-leaf.jpg" runat="server" />
        <img style="border:1px solid black;" src="Protected2/rose-leaf.jpg.cd?width=50" runat="server" />
        <p>There are six images referenced above... 0 should appear.</p>

    </div>
   
    </div>
    </form>
</body>
</html>
