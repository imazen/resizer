<html>
<body>

<%
set r = CreateObject("ImageResizer.Plugins.RemoteReader.RemoteReaderPlugin")
set url = r.CreateSignedUrlWithKey  ("http://www.build.com/imagebase/resized/330x320/minkaaireimages/f518-bn_w_light-.jpg","width=200&height=100", "uy8=89849k87i_uh-dwjgngwty")

response.write("<img src=""" & url & """ />")
%>

</body>
</html>