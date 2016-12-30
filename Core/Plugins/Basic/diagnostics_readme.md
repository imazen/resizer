Tags: plugin
Edition: free
Bundle: free
Tagline: Whenever you have an issue, visit localhost/resizer.debug
Aliases: /plugins/diagnostics /plugins/diagnostic

# Self-diagnostics at /resizer.debug

ImageResizer offers extensive self-diagnostics. With the application running, visit `/resizer.debug` from your browser. 

To view the report, you may need to [adjust customErrors in Web.config](http://msdn.microsoft.com/en-us/library/h0hfz6fc%28v=vs.100%29.aspx). This setting defaults to RemoteOnly, and is case-sensitive. 

If you're accessing the application directly from the server, `RemoteOnly` is best. If you need to troubleshoot remotely, use `Off`, but keep in mind that stack traces can sometimes leak information to attackers.

Setting `customErrors="Off"` will also let you see detailed error messages. We are unable to help you troubleshoot failing requests without the error message and stacktrace.

If an image is failing to display on a web page, you *must* request it directly to get more information. In Chrome, you can "Open Image In New Tab" via the right-click menu (or, better yet, use Dev Tools > Network to locate the failed request details).

Once you have a detailed error message, consult the Troubleshooting page and FAQ. Also note that error messages thrown by ImageResizer typically mean exactly what they say.

### FAQ

* If you're not using IIS Integrated mode, you will need to access diagnostics via `/resizer.debug.ashx`
* You can override diagnostics page visibility without changing customErrors. 
  To override, add one of the following to the &lt;resizer&gt; section.

	  <diagnostics enableFor="AllHosts" />
	  <diagnostics enableFor="Localhost" />
	  <diagnostics enableFor="None" />
  


