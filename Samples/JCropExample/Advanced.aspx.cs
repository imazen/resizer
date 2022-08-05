// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Web.UI;

namespace JCropExample
{
    public partial class Advanced : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            //Here we just do a redirect, but we could store the URL in SQL just as easily, saving the crop settings for future pages.
            if (Page.IsPostBack && !string.IsNullOrEmpty(img1.Value)) Response.Redirect(ResolveUrl(img1.Value));
            //We could even write the cropped image to disk, if we wanted
            //if (Page.IsPostBack && !string.IsNullOrEmpty(img1.Value))  
            //    ImageBuilder.Current.Build("~/" + ImageResizer.Util.PathUtils.RemoveQueryString(img1.Value), "~/cropped.jpg", new ResizeSettings(img1.Value));
        }
    }
}