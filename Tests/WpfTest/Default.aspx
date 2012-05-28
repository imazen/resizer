<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <style type="text/css">
        body { background-color: #f0f0f0; font-family: Segoe UI, Tahoma; }
        ul.level2 li { display: inline-block; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
    <div>
       
        <%--<img src="Images/nasa-1.jpg?width=100&height=100&mode=crop&anchor=bottomright" />
        <img src="Images/nasa-1.jpg?width=300&height=100&mode=crop&anchor=bottomright" /><br />
        <img src="Images/nasa-1.jpg?builder=wpf&width=100&height=100&mode=crop&anchor=bottomright" />
        <img src="Images/nasa-1.jpg?builder=wpf&width=300&height=100&mode=crop&anchor=bottomright" />--%>
        <%--<img src="Images/nasa-1.jpg?builder=wpf&width=100&height=100&crop=(10,10,100,100)" />--%>
        
        <%--<img src="Images/nasa-1.jpg?builder=wpf&width=300&height=100&mode=crop&anchor=bottomright" />--%>

        <ul>
            <li>
                <span>Max</span>
                <img src="Images/nasa-1.jpg?width=300&height=100&mode=max" />
                <img src="Images/nasa-1.jpg?builder=wpf&width=300&height=100&mode=max" />
            </li>
            <li>
                <span>Stretch</span>
                <img src="Images/nasa-1.jpg?width=300&height=100&mode=stretch" />
                <img src="Images/nasa-1.jpg?builder=wpf&width=300&height=100&mode=stretch" />
            </li>
            <li>
                <h1>Crop</h1>
                <h2>Wide images</h2>
                <h3>Standard builder</h3>
                <ul class="level2">
                    <li><img src="Images/nasa-1.jpg?width=100&height=100&mode=crop" /></li>
                    <li><img src="Images/nasa-1.jpg?width=100&height=100&mode=crop&anchor=topleft" /></li>
                    <li><img src="Images/nasa-1.jpg?width=100&height=100&mode=crop&anchor=topcenter" /></li>
                    <li><img src="Images/nasa-1.jpg?width=100&height=100&mode=crop&anchor=topright" /></li>
                    <li><img src="Images/nasa-1.jpg?width=100&height=100&mode=crop&anchor=middleleft" /></li>
                    <li><img src="Images/nasa-1.jpg?width=100&height=100&mode=crop&anchor=middlecenter" /></li>
                    <li><img src="Images/nasa-1.jpg?width=100&height=100&mode=crop&anchor=middleright" /></li>
                    <li><img src="Images/nasa-1.jpg?width=100&height=100&mode=crop&anchor=bottomleft" /></li>
                    <li><img src="Images/nasa-1.jpg?width=100&height=100&mode=crop&anchor=bottomcenter" /></li>
                    <li><img src="Images/nasa-1.jpg?width=100&height=100&mode=crop&anchor=bottomright" /></li>
                    <li><img src="Images/nasa-1.jpg?width=100&height=100&crop=(10,10,100,100)" /></li>
                </ul>
                <h3>Wpf builder</h3>
                <ul class="level2">
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=100&height=100&mode=crop" /></li>
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=100&height=100&mode=crop&anchor=topleft" /></li>
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=100&height=100&mode=crop&anchor=topcenter" /></li>
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=100&height=100&mode=crop&anchor=topright" /></li>
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=100&height=100&mode=crop&anchor=middleleft" /></li>
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=100&height=100&mode=crop&anchor=middlecenter" /></li>
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=100&height=100&mode=crop&anchor=middleright" /></li>
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=100&height=100&mode=crop&anchor=bottomleft" /></li>
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=100&height=100&mode=crop&anchor=bottomcenter" /></li>
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=100&height=100&mode=crop&anchor=bottomright" /></li>
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=100&height=100&crop=(10,10,100,100)" /></li>
                </ul>

                <h2>Tall images</h2>
                <h3>Standard builder</h3>
                <ul class="level2">
                    <li><img src="Images/kt2008_40.jpg?width=100&height=100&mode=crop" /></li>
                    <li><img src="Images/kt2008_40.jpg?width=100&height=100&mode=crop&anchor=topleft" /></li>
                    <li><img src="Images/kt2008_40.jpg?width=100&height=100&mode=crop&anchor=topcenter" /></li>
                    <li><img src="Images/kt2008_40.jpg?width=100&height=100&mode=crop&anchor=topright" /></li>
                    <li><img src="Images/kt2008_40.jpg?width=100&height=100&mode=crop&anchor=middleleft" /></li>
                    <li><img src="Images/kt2008_40.jpg?width=100&height=100&mode=crop&anchor=middlecenter" /></li>
                    <li><img src="Images/kt2008_40.jpg?width=100&height=100&mode=crop&anchor=middleright" /></li>
                    <li><img src="Images/kt2008_40.jpg?width=100&height=100&mode=crop&anchor=bottomleft" /></li>
                    <li><img src="Images/kt2008_40.jpg?width=100&height=100&mode=crop&anchor=bottomcenter" /></li>
                    <li><img src="Images/kt2008_40.jpg?width=100&height=100&mode=crop&anchor=bottomright" /></li>
                </ul>
                <h3>Wpf builder</h3>
                <ul class="level2">
                    <li><img src="Images/kt2008_40.jpg?builder=wpf&width=100&height=100&mode=crop" /></li>
                    <li><img src="Images/kt2008_40.jpg?builder=wpf&width=100&height=100&mode=crop&anchor=topleft" /></li>
                    <li><img src="Images/kt2008_40.jpg?builder=wpf&width=100&height=100&mode=crop&anchor=topcenter" /></li>
                    <li><img src="Images/kt2008_40.jpg?builder=wpf&width=100&height=100&mode=crop&anchor=topright" /></li>
                    <li><img src="Images/kt2008_40.jpg?builder=wpf&width=100&height=100&mode=crop&anchor=middleleft" /></li>
                    <li><img src="Images/kt2008_40.jpg?builder=wpf&width=100&height=100&mode=crop&anchor=middlecenter" /></li>
                    <li><img src="Images/kt2008_40.jpg?builder=wpf&width=100&height=100&mode=crop&anchor=middleright" /></li>
                    <li><img src="Images/kt2008_40.jpg?builder=wpf&width=100&height=100&mode=crop&anchor=bottomleft" /></li>
                    <li><img src="Images/kt2008_40.jpg?builder=wpf&width=100&height=100&mode=crop&anchor=bottomcenter" /></li>
                    <li><img src="Images/kt2008_40.jpg?builder=wpf&width=100&height=100&mode=crop&anchor=bottomright" /></li>
                </ul>

                <h2>With percentages</h2>
                <h3>Standard builder</h3>
                <ul class="level2">
                    <li><img src="Images/nasa-1.jpg?crop=(40,40,80,80)&cropxunits=100&cropyunits=100" /></li>
                </ul>
                <h3>Wpf builder</h3>
                <ul class="level2">
                    <li><img src="Images/nasa-1.jpg?builder=wpf&crop=(40,40,80,80)&cropxunits=100&cropyunits=100" /></li>
                </ul>
                
            </li>
            <li>
                <span>Carve</span>
                <img src="Images/nasa-1.jpg?maxwidth=300&mode=carve" />
            </li>
            <li>
                <h1>Pad</h1>
                <h2>Wide images</h2>
                <h3>Standard builder</h3>
                <ul class="level2">
                    <li><img src="Images/nasa-1.jpg?width=100&height=100&mode=pad&" /></li>
                    <li><img src="Images/nasa-1.jpg?width=100&height=100&mode=pad&anchor=topleft" /></li>
                    <li><img src="Images/nasa-1.jpg?width=100&height=100&mode=pad&anchor=topcenter" /></li>
                    <li><img src="Images/nasa-1.jpg?width=100&height=100&mode=pad&anchor=topright" /></li>
                    <li><img src="Images/nasa-1.jpg?width=100&height=100&mode=pad&anchor=middleleft" /></li>
                    <li><img src="Images/nasa-1.jpg?width=100&height=100&mode=pad&anchor=middlecenter" /></li>
                    <li><img src="Images/nasa-1.jpg?width=100&height=100&mode=pad&anchor=middleright" /></li>
                    <li><img src="Images/nasa-1.jpg?width=100&height=100&mode=pad&anchor=bottomleft" /></li>
                    <li><img src="Images/nasa-1.jpg?width=100&height=100&mode=pad&anchor=bottomcenter" /></li>
                    <li><img src="Images/nasa-1.jpg?width=100&height=100&mode=pad&anchor=bottomright" /></li>
                </ul>
                <h3>Wpf builder</h3>
                <ul class="level2">
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=100&height=100&mode=pad" /></li>
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=100&height=100&mode=pad&anchor=topleft" /></li>
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=100&height=100&mode=pad&anchor=topcenter" /></li>
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=100&height=100&mode=pad&anchor=topright" /></li>
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=100&height=100&mode=pad&anchor=middleleft" /></li>
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=100&height=100&mode=pad&anchor=middlecenter" /></li>
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=100&height=100&mode=pad&anchor=middleright" /></li>
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=100&height=100&mode=pad&anchor=bottomleft" /></li>
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=100&height=100&mode=pad&anchor=bottomcenter" /></li>
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=100&height=100&mode=pad&anchor=bottomright" /></li>
                </ul>

                <h2>Tall images</h2>
                <h3>Standard builder</h3>
                <ul class="level2">
                    <li><img src="Images/kt2008_40.jpg?width=100&height=100&mode=pad" /></li>
                    <li><img src="Images/kt2008_40.jpg?width=100&height=100&mode=pad&anchor=topleft" /></li>
                    <li><img src="Images/kt2008_40.jpg?width=100&height=100&mode=pad&anchor=topcenter" /></li>
                    <li><img src="Images/kt2008_40.jpg?width=100&height=100&mode=pad&anchor=topright" /></li>
                    <li><img src="Images/kt2008_40.jpg?width=100&height=100&mode=pad&anchor=middleleft" /></li>
                    <li><img src="Images/kt2008_40.jpg?width=100&height=100&mode=pad&anchor=middlecenter" /></li>
                    <li><img src="Images/kt2008_40.jpg?width=100&height=100&mode=pad&anchor=middleright" /></li>
                    <li><img src="Images/kt2008_40.jpg?width=100&height=100&mode=pad&anchor=bottomleft" /></li>
                    <li><img src="Images/kt2008_40.jpg?width=100&height=100&mode=pad&anchor=bottomcenter" /></li>
                    <li><img src="Images/kt2008_40.jpg?width=100&height=100&mode=pad&anchor=bottomright" /></li>
                </ul>
                <h3>Wpf builder</h3>
                <ul class="level2">
                    <li><img src="Images/kt2008_40.jpg?builder=wpf&width=100&height=100&mode=pad" /></li>
                    <li><img src="Images/kt2008_40.jpg?builder=wpf&width=100&height=100&mode=pad&anchor=topleft" /></li>
                    <li><img src="Images/kt2008_40.jpg?builder=wpf&width=100&height=100&mode=pad&anchor=topcenter" /></li>
                    <li><img src="Images/kt2008_40.jpg?builder=wpf&width=100&height=100&mode=pad&anchor=topright" /></li>
                    <li><img src="Images/kt2008_40.jpg?builder=wpf&width=100&height=100&mode=pad&anchor=middleleft" /></li>
                    <li><img src="Images/kt2008_40.jpg?builder=wpf&width=100&height=100&mode=pad&anchor=middlecenter" /></li>
                    <li><img src="Images/kt2008_40.jpg?builder=wpf&width=100&height=100&mode=pad&anchor=middleright" /></li>
                    <li><img src="Images/kt2008_40.jpg?builder=wpf&width=100&height=100&mode=pad&anchor=bottomleft" /></li>
                    <li><img src="Images/kt2008_40.jpg?builder=wpf&width=100&height=100&mode=pad&anchor=bottomcenter" /></li>
                    <li><img src="Images/kt2008_40.jpg?builder=wpf&width=100&height=100&mode=pad&anchor=bottomright" /></li>
                </ul>

            </li>
            <li>
                <h1>Format</h1>
                <h2>Jpeg</h2>
                <h3>Standard builder</h3>
                <ul class="level2">
                    <li><img src="Images/nasa-1.jpg?width=150&height=150&mode=pad&format=jpg&bgcolor=transparent" /></li>
                    <li><img src="Images/nasa-1.jpg?width=150&height=150&mode=pad&format=jpg&bgcolor=000000" /></li>
                    <li><img src="Images/nasa-1.jpg?width=150&height=150&mode=pad&format=jpg&bgcolor=00FF00" /></li>
                </ul>
                <h3>Wpf builder</h3>
                <ul class="level2">
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=150&height=150&format=pad&output=jpg&bgcolor=transparent" /></li>
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=150&height=150&format=pad&output=jpg&bgcolor=000000" /></li>
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=150&height=150&format=pad&output=jpg&bgcolor=00FF00" /></li>
                </ul>

                <h2>Png</h2>
                <h3>Standard builder</h3>
                <ul class="level2">
                    <li><img src="Images/nasa-1.jpg?width=150&height=150&mode=pad&format=png&bgcolor=transparent" /></li>
                    <li><img src="Images/nasa-1.jpg?width=150&height=150&mode=pad&format=png&bgcolor=000000" /></li>
                    <li><img src="Images/nasa-1.jpg?width=150&height=150&mode=pad&format=png&bgcolor=00FF00" /></li>
                </ul>
                <h3>Wpf builder</h3>
                <ul class="level2">
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=150&height=150&mode=pad&format=png&bgcolor=transparent" /></li>
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=150&height=150&mode=pad&format=png&bgcolor=000000" /></li>
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=150&height=150&mode=pad&format=png&bgcolor=00FF00" /></li>
                </ul>

                <h2>Gif</h2>
                <h3>Standard builder</h3>
                <ul class="level2">
                    <li><img src="Images/nasa-1.jpg?width=150&height=150&mode=pad&format=gif&bgcolor=transparent" /></li>
                    <li><img src="Images/nasa-1.jpg?width=150&height=150&mode=pad&format=gif&bgcolor=000000" /></li>
                    <li><img src="Images/nasa-1.jpg?width=150&height=150&mode=pad&format=gif&bgcolor=00FF00" /></li>
                </ul>
                <h3>Wpf builder</h3>
                <ul class="level2">
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=150&height=150&mode=pad&format=gif&bgcolor=transparent" /></li>
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=150&height=150&mode=pad&format=gif&bgcolor=000000" /></li>
                    <li><img src="Images/nasa-1.jpg?builder=wpf&width=150&height=150&mode=pad&format=gif&bgcolor=00FF00" /></li>
                </ul>
            </li>
        </ul>
        
    </div>
    </form>
</body>
</html>
