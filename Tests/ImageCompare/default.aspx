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
        <option value="2_computers.gif">2_computers.gif</option>
        
        <option value="rings2.png">rings2.png</option>
        <%= String.Join("\n",new [] {"*.jpg", "*.png", "*.gif"}.SelectMany(ext => System.IO.Directory.GetFiles(System.IO.Path.Combine(MapPath("~/"),@"..\..\Samples\Images\private"), ext)).Select(path => "private/" + System.IO.Path.GetFileName(path)).Select(p => "<option value=\"" + p + "\">" + p + "</option>")) %>
        
    
    </select>

    <input id="newx" value="500" style="width: 30px" onchange="regen();" />px<br /><br />

    <input id="set1" value="fastscale=true&down.filter=lanczos" style="width: 250px" onchange="regen();" /> vs 
    <input id="set2" value="" style="width: 250px" onchange="regen();" />

    <hr />

    <img id="cmp" src="" alt="" onmouseover="set2();" onmouseout="set1();" onload="done();" /><br />
    <input readonly="readonly" id="status" style="width: 1000px" /> <br />
    <a id="link"></a>
    <br />
    <a id="original">original</a>
    <form id="form1" runat="server">
        <%= System.Reflection.Assembly.GetAssembly(typeof(ImageResizer.Plugins.FastScaling.FastScalingPlugin)).GetName().Version.ToString() %>
 Last built
        <%= (DateTime.UtcNow - new System.IO.FileInfo(System.Reflection.Assembly.GetAssembly(typeof(ImageResizer.Plugins.FastScaling.FastScalingPlugin)).Location).LastWriteTimeUtc).Seconds %>
        seconds ago.
    </form>

     <script>
         cmp = document.getElementById("cmp");
         st = document.getElementById("status");

         link = document.getElementById("link");
         original = document.getElementById("original");
         url1 = "";
         url2 = "";
         last = "";

         function regen() {
             pic = document.getElementById("pic").value;
             x = document.getElementById("newx").value;

             s1 = document.getElementById("set1").value;
             s2 = document.getElementById("set2").value;

             r = Math.random();

             original.href = pic + "?cache=always";

             url1 = pic + "?width=" + x + "&" + s1 + "&r=" + r;
             if (s2.substring(0,1) == ":") {
                 url2 = pic.replace(/_org|_orig|_original/, "_").replace(".", s2.replace(/^\:/, "") + ".") + "?cache=always";
             } else {
                 url2 = pic + "?width=" + x + "&" + s2 + "&r=" + r;
             }
             set1();
         }

         function set1() {
             last = url1;
             cmp.src = last;
             link.href = last;
             link.innerText = last;
             st.value = "loading: " + last;
         }

         function set2() {
             last = url2;
             cmp.src = last;
             link.href = last;
             link.innerText = last;
             st.value = "loading: " + last;
         }

         function done() {
             st.value = "showing: " + last;
         }

         regen();
    </script>
</body>
</html>
