<%@ Application Language="C#" %>

<script runat="server">

    void Application_Start(object sender, EventArgs e) 
    {
        // Code that runs on application startup
        ImageResizer.Plugins.Watermark.WatermarkPlugin w = new ImageResizer.Plugins.Watermark.WatermarkPlugin();
        w.align = System.Drawing.ContentAlignment.BottomLeft;
        w.hideIfTooSmall = true;
        w.keepAspectRatio = true;
        w.valuesPercentages = false;
        w.watermarkDir = "~/watermarks/"; //Where the watermark plugin looks for the image specifed in the querystring ?watermark=file.png
        w.bottomRightPadding = new System.Drawing.SizeF(20, 20);
        w.topLeftPadding = new System.Drawing.SizeF(20, 20);
        w.watermarkSize = new System.Drawing.SizeF(30, 30); //The desired size of the watermark, maximum dimensions (aspect ratio maintained if keepAspectRatio = true)
        //Install the plugin
        w.Install(ImageResizer.Configuration.Config.Current); 
    }
    
    void Application_End(object sender, EventArgs e) 
    {
        //  Code that runs on application shutdown

    }
        
    void Application_Error(object sender, EventArgs e) 
    { 
        // Code that runs when an unhandled error occurs

    }

    void Session_Start(object sender, EventArgs e) 
    {
        // Code that runs when a new session is started

    }

    void Session_End(object sender, EventArgs e) 
    {
        // Code that runs when a session ends. 
        // Note: The Session_End event is raised only when the sessionstate mode
        // is set to InProc in the Web.config file. If session mode is set to StateServer 
        // or SQLServer, the event is not raised.

    }
       
</script>
