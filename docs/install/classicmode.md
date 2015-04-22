Aliases: /docs/cleanurls /docs/install/classicmode

# How to get clean URLs on IIS5, IIS6, and IIS7 Classic Mode

**Using IIS7 *Integrated* mode or the Visual Studio web server? You can skip this step.** You also don't need to do this if you are using the **.jpg.ashx** syntax.


**Remember to use the path for YOUR version of asp.net in both IIS and web.config. The safest way is to copy it from the existing mapping for extension '.aspx'.**

## Installing on IIS 5 & IIS 6

For the module to operate properly, we must configure IIS to pass all requests to the ASP.NET runtime. By default IIS serves these files itself.

1) Open IIS, right-click on your web site, and choose properties.

2) Click the "Home Directory" tab, then "Configuration"

3) For a wildcard mapping on IIS 5, add extension ".\*". For IIS 6, choose "Insert" and add the aspnet\_isapi.dll executable *%windir%\Microsoft.NET\Framework\v2.0.50727\aspnet\_isapi.dll* inside the wildcard application maps area. For both IIS 5 and 6, make sure "Script Engine" is checked and "Verify file exists" is unchecked.

Note: For 64-bit installs, use "%windir%\Microsoft.NET\Framework64\v2.0.50727\aspnet\_isapi.dll". The most reliable way to determine the path of aspnet\_isapi.dll is to copy it from the .aspx mapping. You have to use the right-click menus - the keyboard shortcuts don't work.

Note: If you choose to use individual mappings on IIS6 instead of using a wildcard mapping, [you must read and follow this Microsoft KB article file](http://support.microsoft.com/Default.aspx?kbid=909641). These, too, must have "Verify File exists" unchecked.

If you do not uncheck "Verify file exists", the Image404, FolderResizeSyntax, ImageHandlerSyntax, VirtualFolder, Gradient, CloudFront, S3Reader, SqlReader, RemoteReader, AzureReader, PsdReader, and PsdComposer plugins will not work correctly.

If you uncheck "Verify file exists" on a wildcard mapping, the default documents feature of IIS may be disabled, and will need to use a URL rewrite rule to restore the behavior. I suggest downloading the [UrlRewriting.Net](http://www.urlrewriting.net/) library and using it for both default documents functionality and any other URL rewriting you need.

## Installing on IIS 7 classic mode

IIS 7 mode mappings are done in web.config. Do *not* add these if you are using IIS 7 Integrated mode (the default). You may get Server Unavailable on your image requests. None of the steps on this page are needed on IIS7 Integrated (default), and will actually cause problems.

	<configuration>
	    ...
	  <system.webserver>
	    <handlers>
	      <add name="ASPNET" path="*" verb="*" modules="IsapiModule" 
	      scriptprocessor="%windir%\Microsoft.NET\Framework\v2.0.50727\aspnet_isapi.dll" resourcetype="Unspecified" 
	      requireaccess="None" precondition="classicMode,runtimeVersionv2.0,bitness32">
	    </add></handlers>
	  </system.webserver>
	    ...
	</configuration>

### Use this instead on 64-bit machines

	<configuration>
	    ...
	  <system.webserver>
	    <handlers>
	      <add name="ASPNET" path="*" verb="*" modules="IsapiModule" 
	      scriptprocessor="%windir%\Microsoft.NET\Framework64\v2.0.50727\aspnet_isapi.dll" 
	      resourcetype="Unspecified" requireaccess="None" precondition="classicMode,runtimeVersionv2.0,bitness64">
	    </add></handlers>
	  </system.webserver>
	    ...
	</configuration>
