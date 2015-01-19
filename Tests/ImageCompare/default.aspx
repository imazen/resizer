<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="default.aspx.cs" Inherits="ImageCompare._default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <select id="pic" onchange="regen();">
        <option value="premult-test.png">premult-test.png</option>
        <option value="gamma-test.jpg">gamma-test.jpg</option>
        <option value="fountain-small.jpg">fountain-small.jpg</option>
        <option value="red-leaf.jpg">red-leaf.jpg</option>
        <option value="quality-original.jpg">quality-original.jpg</option>
        <option value="rose-leaf.jpg">rose-leaf.jpg</option>
        <option value="Sun_256.png">Sun_256.png</option>
        <option value="rings.png">rings.png</option>
        
        <option value="rings2.png">rings2.png</option>
    </select>

    <input id="newx" value="256" style="width: 30px" onchange="regen();" />px<br /><br />

    <input id="set1" value="fastscale=true&f" style="width: 250px" onchange="regen();" /> vs 
    <input id="set2" value="" style="width: 250px" onchange="regen();" />

    <hr />

    <img id="cmp" src="" alt="" onmouseover="set2();" onmouseout="set1();" onload="done();" /><br />
    <input readonly="readonly" id="status" style="width: 1000px" />

    <form id="form1" runat="server">
        <%= System.Reflection.Assembly.GetAssembly(typeof(ImageResizer.Plugins.FastScaling.FastScalingPlugin)).GetName().Version.ToString() %>
 Last built
        <%= (DateTime.UtcNow - new System.IO.FileInfo(System.Reflection.Assembly.GetAssembly(typeof(ImageResizer.Plugins.FastScaling.FastScalingPlugin)).Location).LastWriteTimeUtc).Seconds %>
        seconds ago.
    </form>

     <script>
         cmp = document.getElementById("cmp");
         st = document.getElementById("status");
         url1 = "";
         url2 = "";
         last = "";

         function regen() {
             pic = document.getElementById("pic").value;
             x = document.getElementById("newx").value;

             s1 = document.getElementById("set1").value;
             s2 = document.getElementById("set2").value;

             r = Math.random();

             url1 = pic + "?width=" + x + "&" + s1 + "&r=" + r;
             url2 = pic + "?width=" + x + "&" + s2 + "&r=" + r;

             set1();
         }

         function set1() {
             last = url1;
             cmp.src = last;
             st.value = "loading: " + last;
         }

         function set2() {
             last = url2;
             cmp.src = last;
             st.value = "loading: " + last;
         }

         function done() {
             st.value = "showing: " + last;
         }

         regen();
    </script>
</body>
</html>
