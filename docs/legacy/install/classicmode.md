Aliases: /docs/cleanurls /docs/install/classicmode

**Using IIS7 *Integrated* mode or the Visual Studio web server? You can skip this step.** You also don't need to do this if you are using the **.jpg.ashx** syntax.


**Remember to use the path for YOUR version of asp.net in both IIS and web.config. The safest way is to copy it from the existing mapping for extension '.aspx'.**

## Installing on IIS 7 classic mode

IIS 7 mode mappings are done in web.config. Do *not* add these if you are using IIS 7 Integrated mode (the default). You may get Server Unavailable on your image requests. None of the steps on this page are needed on IIS7 Integrated (default), and will actually cause problems.

	<configuration>
	    ...
	  <system.webserver>
	    <handlers>
	      <add name="ASPNET" path="*" verb="*" modules="IsapiModule" 
	      scriptprocessor="%windir%\Microsoft.NET\Framework\v4.0.30319\aspnet_isapi.dll" resourcetype="Unspecified" 
	      requireaccess="None" precondition="classicMode,runtimeVersionv4.0,bitness32">
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
	      scriptprocessor="%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_isapi.dll" 
	      resourcetype="Unspecified" requireaccess="None" precondition="classicMode,runtimeVersionv4.0,bitness64">
	    </add></handlers>
	  </system.webserver>
	    ...
	</configuration>


## Older versions of IIS

Please switch to the v3 documentation. ImageResizer v4 requires NET 4.5. 