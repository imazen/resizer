<html>
<body>

<h1>ASP can resize images!</h1>

<p>It's easy to use the URL syntax in ASP - it's no different than in html.</p>

&lt;img src="red-leaf.jpg.ashx?width=100" /&gt;

<img src="red-leaf.jpg.ashx?width=100" >

<h2>You can also resize to disk, using the COM API


<pre>
set c = CreateObject("ImageResizer.Configuration.Config");
c.BuildImage("..\Images\red-leaf.jpg","test-image.jpg","width=100");
</pre>
<%
set c = CreateObject("ImageResizer.Configuration.Config")
c.BuildImage("..\Images\red-leaf.jpg","test-image.jpg","width=100")
%>
<img src="test-image.jpg" />

</body>
</html>